using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Reports.Services;

namespace Reports.Services;

public sealed class ChromeTabsListener : BackgroundService
{
    private readonly ChromeTabsStore _store;
    private HttpListener? _listener;

    // put this in appsettings later
    private const string Token = "CHANGE_ME_TO_RANDOM";

    public ChromeTabsListener(ChromeTabsStore store) => _store = store;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://127.0.0.1:8765/");
        _listener.Start();

        while (!stoppingToken.IsCancellationRequested && _listener.IsListening)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync();
            }
            catch
            {
                break;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    if (ctx.Request.HttpMethod != "POST" || ctx.Request.Url?.AbsolutePath != "/tabs")
                    {
                        ctx.Response.StatusCode = 404;
                        ctx.Response.Close();
                        return;
                    }

                    if (ctx.Request.Headers["X-TabToken"] != Token)
                    {
                        ctx.Response.StatusCode = 401;
                        ctx.Response.Close();
                        return;
                    }

                    using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                    var body = await reader.ReadToEndAsync(stoppingToken);

                    var payload = JsonSerializer.Deserialize<TabsPayload>(body);
                    var urls = payload?.urls ?? new List<string>();

                    // IMPORTANT: update ObservableCollection on UI thread
                    Application.Current.Dispatcher.Invoke(() => _store.ReplaceAll(urls));

                    ctx.Response.StatusCode = 200;
                    ctx.Response.Close();
                }
                catch
                {
                    try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { /* ignore */ }
                }
            }, stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _listener?.Stop();
            _listener?.Close();
        }
        catch { /* ignore */ }

        return base.StopAsync(cancellationToken);
    }

    private sealed class TabsPayload
    {
        public List<string>? urls { get; set; }
    }
}