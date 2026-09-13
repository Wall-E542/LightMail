namespace LightMail.Components;

public class MailAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    
    public bool Connected { get; set; }

    public string? ImapServer { get; set; }
    public int ImapPort { get; set; } = 993;


    public string Initials =>
        string.Join("",
                Name.Split(' ')
                    .Select(x => x[0]))
            .ToUpper();
}