using System.Text.Json;
using LightMail.Components;

public class AccountStorageService
{
    private readonly string _filePath;
    private readonly string _lastAccountPath;

    public AccountStorageService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "LightMail"
        );


        Directory.CreateDirectory(folder);


        _filePath = Path.Combine(folder, "accounts.json");
        _lastAccountPath = Path.Combine(folder, "lastAccount.txt");
    }
    
    public async Task ClearLastAccountAsync()
    {
        if (File.Exists(_lastAccountPath))
            File.Delete(_lastAccountPath);

        await Task.CompletedTask;
    }
    
    public async Task SaveLastAccountAsync(MailAccount account)
    {
        await File.WriteAllTextAsync(
            _lastAccountPath,
            account.Id
        );
    }
    
    public async Task<string?> GetLastAccountIdAsync()
    {
        if (!File.Exists(_lastAccountPath))
            return null;


        return await File.ReadAllTextAsync(_lastAccountPath);
    }

    public async Task SaveAccountsAsync(List<MailAccount> accounts)
    {
        var json = JsonSerializer.Serialize(accounts);

        await File.WriteAllTextAsync(
            _filePath,
            json
        );
    }

    
    public async Task<List<MailAccount>> LoadAccountsAsync()
    {
        if (!File.Exists(_filePath))
            return new List<MailAccount>();


        var json = await File.ReadAllTextAsync(_filePath);


        if (string.IsNullOrWhiteSpace(json))
            return new List<MailAccount>();


        return JsonSerializer.Deserialize<List<MailAccount>>(json)
               ?? new List<MailAccount>();
    }
}