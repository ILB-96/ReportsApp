using System.Diagnostics;
using System.IO;

namespace Reports.Services.Files;

public static class ShellService
{
    public static void OpenDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        Directory.CreateDirectory(path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}