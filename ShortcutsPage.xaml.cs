
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Reports
{
    public partial class ShortcutsPage : Page
    {
        public ShortcutsPage()
        {
            InitializeComponent();
        }



        
        private async void Paid_Click(object sender, RoutedEventArgs e)
                {
                    string brand = (sender as FrameworkElement)?.Tag?.ToString()?.Trim().ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(brand))
                    {
                        await ShowOverlayAsync(false, "שגיאה: לא זוהה מותג הכפתור (Tag).");
                        return;
                    }

                    try
                    {
                        // 1) Resolve template and images by brand
                        string htmlPath   = $"Assets/{brand}_paid_fine.html";
                        string headerPath = $"Assets/{brand}_header.png";

                        // 2) Load HTML from Resource
                        string html = LoadTextResource(htmlPath);

                        // 3) Optional token replacement
                        // html = html.Replace("{CustomerName}", "צורית שמן");

                        // 4) Replace img src for header/footer with Base64 data URIs from Resources
                        html = ReplaceImgSrcWithDataUri(html, $"{brand}_header.png", "image/png", headerPath);
                        // 5) Put on clipboard (HTML + plain-text fallback)
                        var data = new DataObject();
                        data.SetData(DataFormats.Html, BuildClipboardHtml(html));
                        data.SetData(DataFormats.UnicodeText, HtmlToPlainText(html));
                        Clipboard.SetDataObject(data, true);

                        // 6) Success overlay
                        await ShowOverlayAsync(true, $"תבנית {brand} הועתקה ללוח. אפשר להדביק ל‑Word/Outlook/WhatsApp Web.");
                    }
                    catch (Exception ex)
                    {
                        await ShowOverlayAsync(false, $"העתקה ללוח נכשלה ({brand}): {ex.Message}");
                    }
                }

                // ----- Resource helpers -----
                private static string LoadTextResource(string relativePath)
                {
                    var uri = new Uri($"pack://application:,,,/{relativePath}");
                    var res = Application.GetResourceStream(uri)
                              ?? throw new FileNotFoundException("Resource not found", relativePath);
                    using var reader = new StreamReader(res.Stream, Encoding.UTF8);
                    return reader.ReadToEnd();
                }

                private static byte[] LoadBytesResource(string relativePath)
                {
                    var uri = new Uri($"pack://application:,,,/{relativePath}");
                    var res = Application.GetResourceStream(uri)
                              ?? throw new FileNotFoundException("Resource not found", relativePath);
                    using var ms = new MemoryStream();
                    res.Stream.CopyTo(ms);
                    return ms.ToArray();
                }

                private static string ReplaceImgSrcWithDataUri(string html, string fileNameInHtml, string mime, string resourcePath)
                {
                    string base64 = Convert.ToBase64String(LoadBytesResource(resourcePath));
                    string dataUri = $"data:{mime};base64,{base64}";

                    // Replace both quote styles
                    html = html.Replace($"src=\"{fileNameInHtml}\"", $"src=\"{dataUri}\"");
                    html = html.Replace($"src='{fileNameInHtml}'", $"src=\"{dataUri}\"");
                    return html;
                }

                // ----- Keep your clipboard/overlay helpers -----
                private static string BuildClipboardHtml(string htmlBody)
                {
                    const string startMarker = "<!--StartFragment-->";
                    const string endMarker = "<!--EndFragment-->";

                    string htmlDoc =
        $@"<html>
        <head>
        <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
        </head>
        <body>
        {startMarker}
        {htmlBody}
        {endMarker}
        </body>
        </html>";

                    string headerTemplate =
                        "Version:0.9\r\nStartHTML:{0:0000000000}\r\nEndHTML:{1:0000000000}\r\n" +
                        "StartFragment:{2:0000000000}\r\nEndFragment:{3:0000000000}\r\n";

                    string provisionalHeader = string.Format(headerTemplate, 0, 0, 0, 0);
                    int startHTML = provisionalHeader.Length;
                    int endHTML = startHTML + Encoding.UTF8.GetByteCount(htmlDoc);

                    int fragmentStartInHtml = htmlDoc.IndexOf(startMarker, StringComparison.Ordinal);
                    int fragmentEndInHtml = htmlDoc.IndexOf(endMarker, StringComparison.Ordinal);

                    int startFragment = startHTML + Encoding.UTF8.GetByteCount(htmlDoc.Substring(0, fragmentStartInHtml)) + Encoding.UTF8.GetByteCount(startMarker);
                    int endFragment = startHTML + Encoding.UTF8.GetByteCount(htmlDoc.Substring(0, fragmentEndInHtml));

                    string finalHeader = string.Format(headerTemplate, startHTML, endHTML, startFragment, endFragment);
                    return finalHeader + htmlDoc;
                }

                private static string HtmlToPlainText(string html)
                {
                    string s = html.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
                    s = s.Replace("</p>", "\n").Replace("</div>", "\n");
                    s = System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", string.Empty);
                    s = System.Net.WebUtility.HtmlDecode(s);
                    s = System.Text.RegularExpressions.Regex.Replace(s, @"\n{3,}", "\n\n");
                    return s.Trim();
                }

                private async Task ShowOverlayAsync(bool success, string message, int milliseconds = 2500)
                {
                    var successBrush = TryFindResource("SystemFillColorSuccessBrush") as Brush
                                       ?? new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)); // green
                    var dangerBrush  = TryFindResource("SystemFillColorCriticalBrush") as Brush
                                       ?? new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)); // red
                    var textBrush    = TryFindResource("TextOnAccentFillColorPrimaryBrush") as Brush
                                       ?? Brushes.White;

                    OverlayBanner.Background = success ? successBrush : dangerBrush;
                    OverlayText.Foreground   = textBrush;
                    OverlayIcon.Foreground   = textBrush;
                    OverlayIcon.Text         = success ? "✔" : "✖";
                    OverlayText.Text         = message;

                    var show = TryFindResource("ShowOverlayStoryboard") as Storyboard;
                    var hide = TryFindResource("HideOverlayStoryboard") as Storyboard;

                    OverlayBanner.Visibility = Visibility.Visible;

                    if (show != null) show.Begin(); else OverlayBanner.Opacity = 1;
                    await Task.Delay(milliseconds);
                    if (hide != null) hide.Begin(); else OverlayBanner.Opacity = 0;

                    await Task.Delay(220);
                    OverlayBanner.Visibility = Visibility.Collapsed;
                }

    }
}
