using LightMail.Components;
using LightMail.Components.Pages;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using EmailAttachment = LightMail.Components.EmailAttachment;
using EmailMessage = LightMail.Components.EmailMessage;
using FolderAccess = MailKit.FolderAccess;
using IMailFolder = MailKit.IMailFolder;
using MessageSummaryItems = MailKit.MessageSummaryItems;

namespace LightMail.Services;




public class MailService
{
    private SmtpClient? _smtp;
    private ImapClient? _client;
    public event Action? AccountChanged;
    public event Action? SearchChanged;
    public string? searchTerm;
    private readonly AccountStorageService AccountStorage;
    private readonly SemaphoreSlim _imapLock = new(1, 1);
    
    public int UnReadAmount;
    
    public IMailFolder? ArchiveFolder;

    public void SearchTermChanged(string? searchTerm)
    {
        this.searchTerm = searchTerm;
        SearchChanged?.Invoke();
    }
    
    public MailService(
        AccountStorageService accountStorage,
        HtmlMailSanitizer sanitizer)
    {
        AccountStorage = accountStorage;
        Sanitizer = sanitizer;
    }

    private readonly HtmlMailSanitizer Sanitizer;

    public void ClearLastAccount()
    {
        CurrentAccount = null;
    }

    public MailAccount? CurrentAccount { get; private set; }



    public async Task<bool> AutoLoginAsync()
    {
        if (CurrentAccount != null)
        {
            return false;
        }
        var lastId = await AccountStorage.GetLastAccountIdAsync();

        if (lastId == null)
            return false;


        var accounts = await AccountStorage.LoadAccountsAsync();


        var account = accounts
            .FirstOrDefault(x => x.Id == lastId);


        if (account == null)
            return false;


        return await LoginAsync(account);
    }

    public async Task<bool> LoginAsync(MailAccount account)
    {
        try
        {
   
            var new_client = new ImapClient();


            var server = GetImapServer(account.Email);


            await new_client.ConnectAsync(
                server,
                993,
                SecureSocketOptions.SslOnConnect
            );


            await new_client.AuthenticateAsync(
                account.Email,
                account.Password
            );


            account.Connected = true;

            account.ImapServer = server;
            
            _smtp = new SmtpClient();
            server = GetSmtpServer(account.Email);

            await _smtp.ConnectAsync(server,587,SecureSocketOptions.StartTls);

            await _smtp.AuthenticateAsync(
                account.Email,
                account.Password);
            
            _client = new_client;
            CurrentAccount = account;
            AccountChanged?.Invoke();
            //UnReadAmount = await GetUnreadCountAsync();
            ArchiveFolder = await GetArchiveFolderAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }
    
    public async Task SendMailAsync(
        MimeMessage mail)
    {
        await _smtp.SendAsync(mail);
    }

    public async Task<int> GetUnreadCountAsync()
    {
        try
        {
            if (_client == null ||
                !_client.IsConnected ||
                !_client.IsAuthenticated)
                return 0;


            var inbox = _client.Inbox;


            await inbox.OpenAsync(FolderAccess.ReadOnly);


            var messages = await inbox.FetchAsync(
                0,
                -1,
                MessageSummaryItems.Flags);


            return messages.Count(x =>
                x.Flags.HasValue &&
                !x.Flags.Value.HasFlag(MessageFlags.Seen));
        }
        catch(Exception ex)
        {
            Console.WriteLine(
                $"UnreadCount Fehler: {ex.Message}");

            return 0;
        }
    }

    public ImapClient? GetClient()
    {
        return _client;
    }

    private string GetSmtpServer(string email)
    {
        var domain = email.Split('@')[1].ToLower();

        return domain switch
        {
            "gmail.com" => "smtp.gmail.com",
            "web.de" => "smtp.web.de",
            "gmx.de" => "mail.gmx.net",
            "outlook.com" => "smtp-mail.outlook.com",
            _ => "smtp." + domain
        };
    }
    
    private string GetImapServer(string email)
    {
        var domain = email.Split('@')[1].ToLower();

        return domain switch
        {
            "web.de" => "imap.web.de",

            "gmail.com" => "imap.gmail.com",

            "outlook.com" => "imap-mail.outlook.com",

            "gmx.de" => "imap.gmx.net",

            _ => "imap." + domain
        };
    }

    // actual email display stuff

    public async Task<EmailMessage?> GetMailAsync( string folderName, UniqueId Id)
    {
        var mail = new EmailMessage();
        
        await _imapLock.WaitAsync();
        try
        {
            if (_client == null)
                return null;

            IMailFolder folder = await GetFolderAsync(folderName);

            await folder.OpenAsync(FolderAccess.ReadWrite);


            var message = await folder.GetMessageAsync(Id);

            // als gelesen markieren
            await folder.AddFlagsAsync(
                Id,
                MessageFlags.Seen,
                true
            );

            UnReadAmount = await GetUnreadCountAsync();

            var htmlPart = message.BodyParts
                .OfType<TextPart>()
                .FirstOrDefault(x => x.IsHtml);

            var textPart = message.BodyParts
                .OfType<TextPart>()
                .FirstOrDefault(x => x.IsPlain);

            // Attachments
            var attachments = new List<EmailAttachment>();

            foreach (var attachment in message.Attachments)
                if (attachment is MimePart part)
                {
                    using var memory = new MemoryStream();

                    part.Content.DecodeTo(memory);

                    attachments.Add(new EmailAttachment
                    {
                        FileName = part.FileName,
                        ContentType = part.ContentType.MimeType,
                        Size = memory.Length,
                        Data = memory.ToArray()
                    });
                }

            string htmlBody = "";
            string plainBody = "";

            if (htmlPart != null)
            {
                htmlBody = Sanitizer.Sanitize(htmlPart.Text);
            }


            if (textPart != null)
            {
                plainBody = textPart.Text;
            }
            
            mail.Id = Id.ToString();
            mail.Sender = message.From.ToString();
            mail.Recipients = message.To
                .Select(x => x.ToString())
                .ToList();
            mail.Subject = message.Subject;
            mail.Date = message.Date.DateTime;
            mail.Body = htmlBody != ""
                ? htmlBody
                : plainBody;
            mail.PlainBody = plainBody;
            mail.IsHtml = htmlBody != "";
            mail.IsRead = true;
            mail.Attachments = attachments;
        }
        finally
        {
            _imapLock.Release();
        }

        return mail;
    }

    // Mail in den Papierkorb verschieben (bzw. endgültig löschen,
    // falls kein Papierkorb-Ordner ermittelt werden kann)

    public async Task MoveToTrashAsync(Posteingang.EmailPreview email)
    {
        await MoveToTrashAsync(email.Folder, email.Id);
    }

    public async Task MoveToTrashAsync(string folderName, UniqueId ID)
    {
        await _imapLock.WaitAsync();
        try
        {
            if (_client == null || !_client.IsConnected || !_client.IsAuthenticated)
                throw new InvalidOperationException("Kein Konto verbunden.");

            var inbox = await GetFolderAsync(folderName);

            if (!inbox.IsOpen || inbox.Access != FolderAccess.ReadWrite)
                await inbox.OpenAsync(FolderAccess.ReadWrite);

            var trash = await GetTrashFolderAsync();

            if (trash != null)
            {
                await inbox.MoveToAsync(ID, trash);
            }
            else
            {
                await inbox.AddFlagsAsync(ID, MessageFlags.Deleted, true);
                await inbox.ExpungeAsync();
            }
        }
        finally
        {
            _imapLock.Release();
        }
    }
    
    public async Task MoveToTrashAsync(List<Posteingang.EmailPreview> emails)
    {
        await _imapLock.WaitAsync();
        try
        {
            if (emails.Count == 0)
                return;
            if (_client == null || !_client.IsConnected || !_client.IsAuthenticated)
                throw new InvalidOperationException("Kein Konto verbunden.");
            var inbox = GetFolderAsync(emails[0].Folder).Result;

            if (!inbox.IsOpen || inbox.Access != FolderAccess.ReadWrite)
                await inbox.OpenAsync(FolderAccess.ReadWrite);

            var trash = await GetTrashFolderAsync();

            List<UniqueId> indicies = new List<UniqueId>(emails.Count);
            foreach (var email in emails)
            {
            
                indicies.Add(email.Id);
            }
        
            if (trash != null)
            {
                // Verschiebt die Mail in den Papierkorb-Ordner
                await inbox.MoveToAsync(indicies, trash);
            }
            else
            {
                // Kein Papierkorb gefunden -> Mail endgültig löschen
                await inbox.AddFlagsAsync(indicies, MessageFlags.Deleted, true);
                await inbox.ExpungeAsync();
            }
        }
        finally
        {
            _imapLock.Release();
        }
    }

    private static readonly string[] TrashFolderNames =
    {
        "Trash",
        "Papierkorb",
        "Deleted Items",
        "Deleted Messages",
        "Gelöschte Objekte",
        "Gelöschte Elemente"
    };
    
    private static readonly string[] ArchiveFolderNames =
    {
        "Archive",
        "Archiv",
        "All Mail",
        "Alle Nachrichten",
        "All Messages"
    };

    private bool HasArchive()
    {
        var archive = GetArchiveFolderAsync();
        return archive != null;
    }
    
    private async Task<IMailFolder?> GetArchiveFolderAsync()
    {
        if (_client == null)
            return null;
        if(ArchiveFolder != null)
            return ArchiveFolder;

        // Erst über SPECIAL-USE versuchen
        try
        {
            ArchiveFolder = _client.GetFolder(SpecialFolder.Archive);

            return ArchiveFolder;
        }
        catch (NotSupportedException)
        {
        }

        return null;
        /* Fallback über Namen
        try
        {
            var personal = _client.GetFolder(
                _client.PersonalNamespaces[0]);


            var folders = await personal.GetSubfoldersAsync();


            return folders.FirstOrDefault(x =>
                ArchiveFolderNames.Contains(
                    x.Name,
                    StringComparer.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }*/
    }
    
  
    
    public async Task MoveToArchiveAsync(UniqueId Id)
    {
        if (_client == null ||
            !_client.IsConnected ||
            !_client.IsAuthenticated)
            throw new InvalidOperationException(
                "Kein Konto verbunden.");


        var inbox = _client.Inbox;


        await inbox.OpenAsync(
            FolderAccess.ReadWrite);



        var archive = await GetArchiveFolderAsync();


        if (archive == null)
        {
            return;
        }


        await inbox.MoveToAsync(
            Id,
            archive);
    }
    
    public async Task MoveFromArchiveAsync(UniqueId Id)
    {
        if (_client == null ||
            !_client.IsConnected ||
            !_client.IsAuthenticated)
            throw new InvalidOperationException(
                "Kein Konto verbunden.");
        
        var archive = await GetArchiveFolderAsync();
        
        if (archive == null)
            throw new Exception(
                "Archiv nicht gefunden.");

        await archive.OpenAsync(
            FolderAccess.ReadWrite);
        
        await archive.MoveToAsync(
            Id,
            _client.Inbox);
    }

    private async Task<IMailFolder?> GetTrashFolderAsync()
    {
        if (_client == null)
            return null;

        // Zuerst über die Sonderordner-Erkennung (SPECIAL-USE/XLIST) versuchen
        try
        {
            var special = _client.GetFolder(SpecialFolder.Trash);

            if (special != null)
                return special;
        }
        catch (NotSupportedException)
        {
            // Server unterstützt SPECIAL-USE/XLIST nicht,
            // also unten manuell nach dem Ordner suchen
        }

        // Fallback: bekannte Ordnernamen manuell durchsuchen
        try
        {
            var personal = _client.GetFolder(_client.PersonalNamespaces[0]);

            var subfolders = await personal.GetSubfoldersAsync();

            return subfolders.FirstOrDefault(f =>
                TrashFolderNames.Contains(f.Name, StringComparer.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }
    

    
    private static readonly string[] SpamFolderNames =
    {
        "Spam",
        "Junk",
        "Junk Email",
        "Bulk Mail"
    };


    private async Task<IMailFolder?> GetSpamFolderAsync()
    {
        if (_client == null)
            return null;

        // Zuerst über die Sonderordner-Erkennung (SPECIAL-USE/XLIST) versuchen
        try
        {
            var special = _client.GetFolder(SpecialFolder.Junk);

            if (special != null)
                return special;
        }
        catch (NotSupportedException)
        {
            // Server unterstützt SPECIAL-USE/XLIST nicht,
            // also unten manuell nach dem Ordner suchen
        }
        return null;
    }

    public async Task<List<Posteingang.EmailPreview>> GetPreviewMails(string folderName)
    {
        var result = new List<Posteingang.EmailPreview>();
        await _imapLock.WaitAsync();
        try
        {
            if (_client == null || !_client.IsConnected || !_client.IsAuthenticated)
                throw new InvalidOperationException("Kein Konto verbunden.");
            
            var folder = await GetFolderAsync(folderName);

            if (folder == null)
                return result;

            await folder.OpenAsync(FolderAccess.ReadOnly);

            var messages = await folder.FetchAsync(
                0,
                -1,
                MessageSummaryItems.Envelope |
                MessageSummaryItems.Flags |
                MessageSummaryItems.UniqueId);

            foreach (var mail in messages)
            {
                result.Add(new Posteingang.EmailPreview
                {
                    Id = mail.UniqueId,

                    Folder = folderName,

                    Sender = mail.Envelope.From.ToString(),

                    Subject = mail.Envelope.Subject ?? "",

                    Date = mail.Envelope.Date?.DateTime ?? DateTime.Now,

                    IsRead = mail.Flags.HasValue &&
                             mail.Flags.Value.HasFlag(MessageFlags.Seen)
                });
            }

        }
        finally
        {
            _imapLock.Release();
        }

        return result;
    }
    
    public async Task<List<Posteingang.EmailPreview>> GetArchiveAsync()
    {
        return await GetPreviewMails("Archive");
    }
    
    public async Task<List<Posteingang.EmailPreview>> GetInboxAsync()
    {
        return await GetPreviewMails("Inbox");
    }
    
    public async Task<List<Posteingang.EmailPreview>> GetTrashAsync()
    {
        return await GetPreviewMails("Trash");
    }
    public async Task<List<Posteingang.EmailPreview>> GetSpamAsync()
    {
        return await GetPreviewMails("Spam");
    }
    
    public async Task<List<Posteingang.EmailPreview>> GetSentAsync()
    {
        return await GetPreviewMails("Sent");
    }
    
    public async Task<List<Posteingang.EmailPreview>> GetDraftsAsync()
    {
        return await GetPreviewMails("Drafts");
    }
    
    public async Task<List<Posteingang.EmailPreview>> GetFlaggedAsync()
    {
        if (_client == null || !_client.IsConnected || !_client.IsAuthenticated)
            throw new InvalidOperationException("Kein Konto verbunden.");
        var result = new List<Posteingang.EmailPreview>();

        var inbox = _client!.Inbox;

        await inbox.OpenAsync(FolderAccess.ReadOnly);


        var messages = await inbox.FetchAsync(
            0,
            -1,
            MessageSummaryItems.Envelope |
            MessageSummaryItems.Flags| 
            MessageSummaryItems.UniqueId
        );


        foreach(var mail in messages)
        {
            if(mail.Flags.HasValue &&
               mail.Flags.Value.HasFlag(MessageFlags.Flagged))
            {
                result.Add(new Posteingang.EmailPreview
                {
                    Id = mail.UniqueId,
                    Sender = mail.Envelope.From.ToString(),
                    Subject = mail.Envelope.Subject ?? "",
                    Date = mail.Envelope.Date?.DateTime ?? DateTime.Now,
                    IsRead = mail.Flags.Value.HasFlag(MessageFlags.Seen)
                });
            }
        }


        return result;
    }
    
    
    private async Task<IMailFolder> GetFolderAsync(string folder)
    {
        return folder switch
        {
            "Inbox" => _client!.Inbox,


            "Spam" => await GetSpamFolderAsync()
                      ?? _client!.Inbox,


            "Trash" => await GetTrashFolderAsync()
                       ?? _client!.Inbox,


            "Archive" => await GetArchiveFolderAsync()
                         ?? _client!.Inbox,
            
            "Sent" => await GetSentFolderAsync()
                      ?? _client!.Inbox,
            "Drafts" => await GetDraftFolderAsync()
                        ?? _client!.Inbox,


            _ => _client!.Inbox,
        };
    }
    
    public async Task<bool> IsFlaggedAsync(
        string folderName,
        UniqueId Id)
    {

        MessageFlags? flags = null;
        await _imapLock.WaitAsync();
        try
        {
            var folder = await GetFolderAsync(folderName);

            await folder.OpenAsync(FolderAccess.ReadOnly);


            var summaries = await folder.FetchAsync(
                new[] { Id },
                MessageSummaryItems.Flags);


            flags = summaries.FirstOrDefault()?.Flags;

        }
        finally
        {
            _imapLock.Release();
        }

        return flags.HasValue &&
               flags.Value.HasFlag(MessageFlags.Flagged);
    }
    
    public async Task SetFlagAsync(
        string folderName,
        UniqueId Id,
        bool value)
    {
        await _imapLock.WaitAsync();
        try
        {
            var folder = await GetFolderAsync(folderName);


            await folder.OpenAsync(FolderAccess.ReadWrite);


            if (value)
            {
                await folder.AddFlagsAsync(
                    Id,
                    MessageFlags.Flagged,
                    true);
            }
            else
            {
                await folder.RemoveFlagsAsync(
                    Id,
                    MessageFlags.Flagged,
                    true);
            }
        }
        finally
        {
            _imapLock.Release();
        }
    }

    public async Task DeleteForeverAsync(
        string folderName,
        UniqueId Id)
    {
        await _imapLock.WaitAsync();
        try
        {
            var folder = await GetFolderAsync(folderName);


            await folder.OpenAsync(FolderAccess.ReadWrite);


            await folder.AddFlagsAsync(
                Id,
                MessageFlags.Deleted,
                true);


            await folder.ExpungeAsync();
        }
        finally
        {
            _imapLock.Release();
        }
    }

    public async Task DeleteForeverMultipleAsync(string folderName, List<Posteingang.EmailPreview> emails)
    {
        await _imapLock.WaitAsync();
        try
        {
            var folder = await GetFolderAsync(folderName);

            await folder.OpenAsync(FolderAccess.ReadWrite);

            foreach (var email in emails)
            {
                await folder.AddFlagsAsync(
                    email.Id,
                    MessageFlags.Deleted,
                    true);
            }
        
            await folder.ExpungeAsync();
        }
        finally
        {
            _imapLock.Release();
        }
    }
    
    private static readonly string[] SentFolderNames =
    {
        "Sent",
        "Sent Items",
        "Sent Messages",
        "Gesendet",
        "Gesendete Elemente",
        "Gesendete Nachrichten"
    };
    
    private async Task<IMailFolder?> GetSentFolderAsync()
    {
        if (_client == null)
            return null;


        try
        {
            var special = _client.GetFolder(
                SpecialFolder.Sent);

            if (special != null)
                return special;
        }
        catch(NotSupportedException)
        {

        }


        try
        {
            var personal = _client.GetFolder(
                _client.PersonalNamespaces[0]);


            var folders = await personal.GetSubfoldersAsync();


            return folders.FirstOrDefault(x =>
                SentFolderNames.Contains(
                    x.Name,
                    StringComparer.OrdinalIgnoreCase));

        }
        catch
        {
            return null;
        }
    }
    
    // Entwürfe
    private static readonly string[] DraftFolderNames =
    {
        "Drafts",
        "Draft",
        "Entwürfe",
        "Entwurf"
    };
    private async Task<IMailFolder?> GetDraftFolderAsync()
    {
        if (_client == null)
            return null;
        try
        {
            var special = _client.GetFolder(
                SpecialFolder.Drafts);
            if(special != null)
                return special;
        }
        catch(NotSupportedException)
        {

        }
        try
        {
            var personal = _client.GetFolder(
                _client.PersonalNamespaces[0]);
            var folders = await personal.GetSubfoldersAsync();
            return folders.FirstOrDefault(x =>
                DraftFolderNames.Contains(
                    x.Name,
                    StringComparer.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }
    
    
    public async Task SaveDraftAsync(
        MimeMessage mail)
    {
        var drafts = await GetDraftFolderAsync();


        if(drafts == null)
            throw new Exception(
                "Kein Entwurfsordner gefunden.");



        await drafts.OpenAsync(
            FolderAccess.ReadWrite);



        await drafts.AppendAsync(
            mail,
            MessageFlags.Draft);
    }
}