using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.Hosting;

namespace Reports.Services.ChromeSync;

public sealed class ChromeTabsListener(ChromeSyncStore store) : BackgroundService
{
    private HttpListener? _listener;

    private const string Token = "CHANGE_ME_TO_RANDOM";

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
                    if (ctx.Request.HttpMethod != "POST" || ctx.Request.Url?.AbsolutePath != "/chrome-sync")
                    {
                        ctx.Response.StatusCode = 404;
                        ctx.Response.Close();
                        return;
                    }

                    if (!string.Equals(ctx.Request.Headers["X-TabToken"], Token, StringComparison.Ordinal))
                    {
                        ctx.Response.StatusCode = 401;
                        ctx.Response.Close();
                        return;
                    }

                    using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                    var body = await reader.ReadToEndAsync(stoppingToken);

                    var payload = JsonSerializer.Deserialize<ChromeSyncPayload>(body, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var urls = payload?.Urls ?? new List<string>();
                    var cookiesByOrigin = payload?.CookiesByOrigin
                                          ?? new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        store.ReplaceAll(urls, cookiesByOrigin);
                    });

                    ctx.Response.StatusCode = 200;
                    ctx.Response.Close();
                }
                catch
                {
                    try
                    {
                        ctx.Response.StatusCode = 500;
                        ctx.Response.Close();
                    }
                    catch
                    {
                    }
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
        catch
        {
        }

        return base.StopAsync(cancellationToken);
    }
}

public sealed class ChromeSyncPayload
{
    public string? UpdatedAt { get; init; }
    public List<string>? Urls { get; init; }
    public Dictionary<string, Dictionary<string, string>>? CookiesByOrigin { get; init; }
}