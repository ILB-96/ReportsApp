
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;


namespace Reports
{
    public partial class ShortcutsPage
    {
        public ShortcutsPage()
        {
            InitializeComponent();
        }


        private async void Transport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string htmlPath   = $"Assets/transport_fine.html";
                string html = LoadTextResource(htmlPath);
            
                var data = new DataObject();
                data.SetData(DataFormats.Html, BuildClipboardHtml(html));
                data.SetData(DataFormats.UnicodeText, HtmlToPlainText(html));
                Clipboard.SetDataObject(data, true);

                // 6) Success overlay
                await Overlay.ShowAsync(true, $"תבנית תחבצ הועתקה ללוח. אפשר להדביק.");
            }
            catch (Exception ex)
            {
                await Overlay.ShowAsync(false, ex.Message);
                throw;
            }
        }

        private async void Paid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var brand = (sender as FrameworkElement)?.Tag?.ToString()?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(brand))
                {
                    await Overlay.ShowAsync(false, "שגיאה: לא זוהה מותג הכפתור (Tag).");
                    return;
                }

                try
                {
                    // 1) Resolve template and images by brand
                    var htmlPath   = $"Assets/{brand}_paid_fine.html";
                    var headerPath = $"Assets/{brand}_header.png";

                    // 2) Load HTML from Resource
                    var html = LoadTextResource(htmlPath);

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
                    await Overlay.ShowAsync(true, $"תבנית {brand} הועתקה ללוח. אפשר להדביק.");
                }
                catch (Exception ex)
                {
                    await Overlay.ShowAsync(false, $"העתקה ללוח נכשלה ({brand}): {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                await Overlay.ShowAsync(false, ex.Message);
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

              

    }
}
