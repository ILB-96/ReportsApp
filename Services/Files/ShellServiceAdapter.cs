namespace Reports.Services.Files;

public sealed class ShellServiceAdapter : IShellService
{
    public void OpenDirectory(string path)
    {
        ShellService.OpenDirectory(path);
    }
}