namespace Reports.Services.Files;
public interface IShellService
{
    void OpenDirectory(string path);
}
public sealed class ShellServiceAdapter : IShellService
{
    public void OpenDirectory(string path)
    {
        ShellService.OpenDirectory(path);
    }
}