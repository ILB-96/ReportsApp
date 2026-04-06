namespace Reports.Utilities;
using System.IO;
using System.Text.Json;

public static class UserSettings
{
    private static readonly string FilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userSettings.json");

    public static string LastCookie { get; set; } = "";

    public static void Load()
    {
        if (!File.Exists(FilePath))
            File.WriteAllText(FilePath, JsonSerializer.Serialize(new UserSettingsDto()));
        
        var json = File.ReadAllText(FilePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            LastCookie = "";
            return;
        }

        var data = JsonSerializer.Deserialize<UserSettingsDto>(json);
        LastCookie = data?.LastCookie ?? "";
    }

    private static bool IsNewCookies(string cookies)
    {
        return string.IsNullOrWhiteSpace(LastCookie) || cookies != LastCookie;
    }

    public static void Save(string cookies)
    {
        if (string.IsNullOrWhiteSpace(cookies) || !IsNewCookies(cookies))
        {
            return;
        }
        LastCookie = cookies;
        
        var json = JsonSerializer.Serialize(new UserSettingsDto
        {
            LastCookie = LastCookie
        });

        File.WriteAllText(FilePath, json);
    }

    private class UserSettingsDto
    {
        public string LastCookie { get; set; } = "";
    }
}