using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Reports.Services;
using Reports.Services.ChromeSync;
using Reports.Services.Crm;
using Reports.Utilities;

namespace Reports.Tabs
{
    public partial class ShortcutsPage
    {
        private readonly ICrmBrandResolver _brandResolver;
        private readonly ICrmCookieProvider _cookieProvider;
    
        public ChromeSyncStore SyncStore { get; }

        public ShortcutsPage(
            ChromeSyncStore syncStore,
            ICrmBrandResolver brandResolver,
            ICrmCookieProvider cookieProvider)
        {
            InitializeComponent();
            SyncStore = syncStore;
            _brandResolver = brandResolver;
            _cookieProvider = cookieProvider;
            DataContext = this;
        }
        
        private string FillTemplate(string html, Dictionary<string, string> values)
        {
            return values.Aggregate(html, (current, pair) => current.Replace("{" + pair.Key + "}", pair.Value ?? ""));
        }

        private async void Copy_Dynamic_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileName = (sender as FrameworkElement)?.Tag?.ToString()?.Trim().ToLowerInvariant();
                var brand = _brandResolver.ServiceTypeFromUrl(Url.Text);
                var htmlPath = $"Assets/{fileName}.html";
                var headerPath = $"Assets/{brand}_header.png";

                // Load raw HTML
                var html = LoadTextResource(htmlPath);

                
                var baseUri = _brandResolver.BaseUri(brand);
                
                var cookies = _cookieProvider.GetCookiesForUrl(Url.Text.Trim());
        
                if (cookies.Count == 0)
                    throw new InvalidOperationException("No CRM cookies were found for this URL. Open the CRM tab in Chrome and try again.");

                using var crm = new CrmApi(CrmClientFactory.Create(baseUri, cookies));
                var incidentId = crm.ExtractCrmId(Url.Text.Trim());
                 

                var incident = await crm.GetIncidentAsync(incidentId);
                var (accountId, isLeasing, contactId, partial) = CrmParsers.ParseIncident(incident, brand);
                html = ReplaceImgSrcWithDataUri(html, $"{brand}_header.png", "image/png", headerPath);
                brand = partial.IsLeasing ? "leasing" : brand;
                var footerPath = $"Assets/{brand}_footer.png";
                Console.WriteLine($"Trying to load resource: {footerPath}");
                html = ReplaceImgSrcWithDataUri(html, $"{brand}_footer.png", "image/png", footerPath);
                Console.WriteLine($"Trying to load resource: {footerPath}");
                if (string.IsNullOrWhiteSpace(accountId))
                    throw new InvalidOperationException("Account Link is missing.");
            

                var account = (!isLeasing && !string.IsNullOrWhiteSpace(contactId)) ? await crm.GetContactAsync(contactId) : await crm.GetAccountAsync(accountId);
                var crmData = CrmParsers.MergeAccount(partial, account);
                
                var values = new Dictionary<string, string>
                {
                    ["FullName"] = crmData.AccountFullName,
                    ["Fine"] = crmData.ReportNumber,
                    ["Car"] = crmData.CarLicense,
                    ["Date"] = crmData.ReportTime.Split(" ")[0],
                    ["Time"] = crmData.ReportTime.Split(" ")[1],
                    ["Reservation"] = string.IsNullOrWhiteSpace(crmData.ReservationNumber) ? "ליסינג" : crmData.ReservationNumber,
                    ["Address"] = crmData.ReportAddress,
                    ["Cost"] = CalculatePrice(crmData.ReportPrice),
                    ["ReducedCost"] = CalculateReducedPrice(crmData.ReportPrice),
                    ["Municipality"] = crmData.ReportCity
                };

                // Replace placeholders
                html = FillTemplate(html, values);
                // Copy to clipboard
                var data = new DataObject();
                data.SetData(DataFormats.Html, BuildClipboardHtml(html));
                data.SetData(DataFormats.UnicodeText, HtmlToPlainText(html));
                Clipboard.SetDataObject(data, true);

                await Overlay.ShowAsync(true, $"{fileName} הועתקה ללוח. אפשר להדביק.");
            }
            catch (Exception ex)
            {
                await Overlay.ShowAsync(false, ex.Message);
            }
        }

        private static string CalculatePrice(string crmDataReportPrice)
        {
            if (string.IsNullOrWhiteSpace(crmDataReportPrice))
            {
                return string.Empty;
            }
            var symbol = crmDataReportPrice.Split(" ")[0];
            var cost = crmDataReportPrice.Split(" ")[1];
            return $"{symbol}{float.Parse(cost)}";
        }

        private static string CalculateReducedPrice(string crmDataReportPrice)
        {
            Console.WriteLine(crmDataReportPrice);
            if (string.IsNullOrWhiteSpace(crmDataReportPrice))
            {
                return string.Empty;
            }

            var symbol = crmDataReportPrice.Split(" ")[0];
            var cost = crmDataReportPrice.Split(" ")[1];
            return $"{symbol}{float.Parse(cost) / 4}";
        }


        private async void Copy_Template_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileName = (sender as FrameworkElement)?.Tag?.ToString()?.Trim().ToLowerInvariant();
                var htmlPath   = $"Assets/{fileName}.html";
                var html = LoadTextResource(htmlPath);
            
                var data = new DataObject();
                data.SetData(DataFormats.Html, BuildClipboardHtml(html));
                data.SetData(DataFormats.UnicodeText, HtmlToPlainText(html));
                Clipboard.SetDataObject(data, true);

                // 6) Success overlay
                await Overlay.ShowAsync(true, $"{fileName} הועתקה ללוח. אפשר להדביק.");
            }
            catch (Exception ex)
            {
                await Overlay.ShowAsync(false, ex.Message);
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
                    var htmlPath   = $"Assets/paid_fine.html";
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
                    var srcType = fileNameInHtml.Split('_').Last();
                    // Replace both quote styles
                    html = html.Replace($"src=\"{srcType}\"", $"src=\"{dataUri}\"");
                    html = html.Replace($"src='{srcType}'", $"src=\"{dataUri}\"");
                    return html;
                }

                // ----- Keep your clipboard/overlay helpers -----
                private static string BuildClipboardHtml(string htmlBody)
                {
                    const string startMarker = "<!--StartFragment-->";
                    const string endMarker = "<!--EndFragment-->";

                    string htmlDoc =
        $@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
        <html lang=""he"" xmlns=""http://www.w3.org/1999/xhtml"">
        <head>
                <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8""/>
                <meta http-equiv=""X-UA-Compatible"" content=""IE=edge""/>
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
                <title>Police fine email</title>
                <style type=""text/css"">
                body {{
                        margin: 0;
                }}
                table {{
                        border-spacing: 0;
                }}
                td {{
                        padding: 0;
                }}
                img {{
                        border: 0;
                }}
                </style>
        </head><body>
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
