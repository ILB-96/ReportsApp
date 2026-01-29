using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Reports.Utilities;
using ClosedXML.Excel;
using Reports.Data;

using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;


namespace Reports
{
    public partial class CreateDriverForm : Page
    {
        private readonly AppDb _db;
        private readonly PhonesRepository _phones;

        public CreateDriverForm()
        {
            InitializeComponent();
            

            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Docs", "Drivers");

            var dbPath = Path.Combine(root, "app.sqlite");

            _db = new AppDb(dbPath);
            _phones = new PhonesRepository(_db);

            _ = _phones.EnsureCreatedAsync();
        }
        private void ClearExcelMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { ContextMenu: not null } fe)
            {
                fe.ContextMenu.PlacementTarget = fe;
                fe.ContextMenu.IsOpen = true;
            }
        }
        private async void ClearExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Loading.ShowAsync("מוחק את השורות... רגע סבלנות", "זה יכול לקחת עד כמה שניות...");
                if (sender is not MenuItem mi) return;


                var serviceType = mi.Tag?.ToString();
                if (string.IsNullOrWhiteSpace(serviceType)) return;
                var confirm = MessageBox.Show(
                    $"זה ימחק את כל השורות בקובץ {serviceType}_drivers_export.xlsx.\nלהמשיך?",
                    "אישור מחיקה",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                await Task.Run(() => ClearDriversExportFile(serviceType));
                await Overlay.ShowAsync(true, "נמחק בהצלחה.");
            }
            catch (Exception ex)
            {
                await Overlay.ShowAsync(false, ex.Message);
            }
            finally
            {
                await Loading.HideAsync();
            }
        }

        private async void ClearDriversExportFile(string serviceType)
        {
            try
            {
                var driversFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Docs", "Drivers");

                var excelPath = Path.Combine(driversFolder, $"{serviceType}_drivers_export.xlsx");

                if (!File.Exists(excelPath))
                    await Overlay.ShowAsync(false,$"הקובץ לא נמצא:{excelPath}");

                using var workbook = new XLWorkbook(excelPath);
                var ws = workbook.Worksheet(1);

                var lastRowUsed = ws.LastRowUsed();
                if (lastRowUsed == null)
                {
                    workbook.Save();
                    return;
                }

                var lastRow = lastRowUsed.RowNumber();
                if (lastRow <= 1) // only header row
                {
                    workbook.Save();
                    return;
                }

                // Clear contents from row 2 down, keep header row + formatting
                const int lastCol = 12; // you write up to column 12
                ws.Range(2, 1, lastRow, lastCol).Clear(XLClearOptions.Contents);

                workbook.Save();
            }
            catch (Exception ex)
            {
                await Overlay.ShowAsync(false, ex.Message);
            }
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Loading.ShowAsync("מיצא את פרטי הנהג... רגע סבלנות", "זה יכול לקחת עד כמה שניות...");
                var toggle = ToggleOption.IsChecked == true ? "goto" : "autotel";
                var baseUri = new Uri($"https://{toggle}.crm4.dynamics.com");
                var url = Url.Text.Trim();
                var cookie = Cookies.Text.Trim();
                var cookies = CookieExtractor.ExtractCrmOwinCookies(cookie);
                var incidentHttp = CreateCrmClient(baseUri, cookies);
                var incidentId = url.Contains("&id=") ? url.Split("&id=")[1] : url;
                var incidentPath = $"/api/data/v9.0/incidents({incidentId})";
                var incident = await GetEntityAsync(incidentHttp, incidentPath, "incident.json");
                var accountLink = incident?["_customerid_value"].ToString();
                var accountFullName = incident?["_customerid_value@OData.Community.Display.V1.FormattedValue"];
                var reservationNumber = incident?["c2g_responsibledriverreservationid"];
                var reportTime = incident?["c2g_executiondate@OData.Community.Display.V1.FormattedValue"];
                var isLeasing = incident?["gtg_leasingreport"];
                var reportNumber = incident?["c2g_reportnumber"];
                var carLicense = incident?["_new_vehicle_value@OData.Community.Display.V1.FormattedValue"];

                if (string.IsNullOrWhiteSpace(accountLink))
                    throw new InvalidOperationException("Account Link is missing.");

                var accountHttp = CreateCrmClient(baseUri, cookies);
                var accountPath = $"/api/data/v9.0/accounts({accountLink})";
                var account = await GetEntityAsync(accountHttp, accountPath, "account.json");

                var driverId = new string(
                    (TryGetValue(account, ["c2g_idno", "c2g_privatecompanyno"]))
                    .Where(char.IsDigit)
                    .ToArray());
                var licenseRaw = TryGetValue(account, ["c2g_licenseno"]);
                var driverLicense = new string(licenseRaw.Where(char.IsDigit).ToArray());
                var email = account?["emailaddress1"];
                var phoneNumberRaw = TryGetValue(account, ["address2_telephone1", "c2g_pointofcontacttelephone"]);
                var phoneNumber = phoneNumberRaw.Replace("+", "").Trim() ?? string.Empty;
                var address = account?["address1_line1"];
                var addressHouse2 = account?["address2_line1"];
                var addressCity = account?["address1_city"];
                var addressPostalCode = account?["address1_postalcode"];
                var createdOn = account?["createdon@OData.Community.Display.V1.FormattedValue"];
                var licenseFrontLink = account?["c2g_driverlicensefront"];
                var passportLink = account?["gtg_passportlink"];

                if (await _phones.ExistsAsync(phoneNumber))
                {
                    await Overlay.ShowAsync(false, "This phone already exists locally (SQLite).");
                    return;
                }

                IsLeasing.Text = isLeasing?.ToString() ?? string.Empty;
                ReportStartDateBox.Text = reportTime?.ToString() ?? string.Empty;
                ReportEndDateBox.Text = reportTime?.ToString() ?? string.Empty;
                CarLicenseBox.Text = carLicense?.ToString() ?? string.Empty;
                ReservationNumberBox.Text = reservationNumber?.ToString() ?? string.Empty;
                ReportNumberBox.Text = reportNumber?.ToString() ?? string.Empty;

                AccountFullNameBox.Text = accountFullName?.ToString() ?? string.Empty;
                DriverIdBox.Text = driverId?.ToString() ?? string.Empty;
                DriverLicenseBox.Text = driverLicense?.ToString() ?? string.Empty;
                EmailBox.Text = email?.ToString() ?? string.Empty;
                PhoneBox.Text = phoneNumber?.ToString() ?? string.Empty;

                AddressBox.Text = address?.ToString() ?? string.Empty;
                HouseBox.Text = addressHouse2?.ToString() ?? string.Empty;
                CityBox.Text = addressCity?.ToString() ?? string.Empty;
                PostalCodeBox.Text = addressPostalCode?.ToString() ?? string.Empty;
                CreatedOnBox.Text = createdOn?.ToString() ?? string.Empty;

                LicenseLinkBox.Text = licenseFrontLink?.ToString() ?? string.Empty;
                PassportLinkBox.Text = passportLink?.ToString() ?? string.Empty;

                // Switch UI state
                ShowSecondPage();
            }
            catch (Exception ex)
            {
                await Overlay.ShowAsync(false, ex.Message);
            }
            finally
            {
                await Loading.HideAsync();
            }
        }
        private static string? GetString(Dictionary<string, object>? dict, string key)
        {
            if (dict == null) return null;
            if (!dict.TryGetValue(key, out var value)) return null;

            if (value is JsonElement je)
            {
                return je.ValueKind switch
                {
                    JsonValueKind.String => je.GetString(),
                    JsonValueKind.Null => null,
                    _ => je.ToString()
                };
            }

            return value.ToString();
        }

        private static string TryGetValue(Dictionary<string, object>? dict, IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                var val = GetString(dict, key) ??  string.Empty;
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }

            return string.Empty;
        }
        private HttpClient CreateCrmClient(Uri baseUri, IDictionary<string, string> cookies)
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            foreach (var (name, value) in cookies)
            {
                handler.CookieContainer.Add(
                    baseUri,
                    new Cookie(name, value) { Path = "/", Secure = true }
                );
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = baseUri
            };

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add(
                "Prefer",
                "odata.include-annotations=\"OData.Community.Display.V1.FormattedValue\"");

            return client;
        }

        private async Task<Dictionary<string, object>?> GetEntityAsync(
            HttpClient client,
            string path,
            string? dumpFile = null)
        {
            var resp = await client.GetAsync(path);

            Console.WriteLine($"GET {path} → {(int)resp.StatusCode} {resp.ReasonPhrase}");

            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();

            if (dumpFile != null)
            {
                await File.WriteAllTextAsync(dumpFile, json);
                Console.WriteLine($"JSON saved to: {dumpFile}");
            }

            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        
        private async void SendFinal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Loading.ShowAsync("מייצר נהג... רגע סבלנות", "זה יכול לקחת עד כמה שניות...");
                var toggle = ToggleOption.IsChecked == true ? "goto" : "autotel";
                var isLeasing = bool.TryParse(IsLeasing.Text?.Trim(), out var b) && b;
                var serviceType = isLeasing ? "leasing" : toggle;
                var licenseLink = LicenseLinkBox.Text.Trim();
                var passportLink = PassportLinkBox.Text.Trim();
                
                var excelRow = new Dictionary<int, string>
                {
                    [1] = CarLicenseBox.Text.Trim().Replace("-", ""),
                    [2] = AccountFullNameBox.Text.Trim(),
                    [3]  = DriverIdBox.Text.Trim(),
                    [4]  = PhoneBox.Text.Trim(),
                    [5]  = ReportStartDateBox.Text.Trim(),
                    [6]  = ReportEndDateBox.Text.Trim(),
                    [7]  = DriverLicenseBox.Text.Trim(),
                    [8]  = AddressBox.Text.Trim(),
                    [9]  = HouseBox.Text.Trim(),
                    [10] = CityBox.Text.Trim(),
                    [11] = EmailBox.Text.Trim(),
                    [12] = PostalCodeBox.Text.Trim(),
                };
                if (excelRow[5] == excelRow[6])
                {
                    await Overlay.ShowAsync(false, "שנה טווח חוזה.", 4000);
                    return;
                }
                foreach (var kvp in excelRow.Where(kvp => string.IsNullOrWhiteSpace(kvp.Value)))
                {
                    await Overlay.ShowAsync(false, $"חסרה עמודה {kvp.Key}.", 4000);
                    return;
                }
                
                var driversFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Docs",
                    "Drivers");
                var accountFolder = Path.Combine(driversFolder, $"{excelRow[2]} - {excelRow[1]}");
                var excelPath = Path.Combine(driversFolder, $"{serviceType}_drivers_export.xlsx");
  
                Directory.CreateDirectory(driversFolder);
                Directory.CreateDirectory(accountFolder);
                
                await DownloadIfExistsAsync(licenseLink, accountFolder, "license");
                await DownloadIfExistsAsync(passportLink, accountFolder, "passport");

                OpenDirectory(accountFolder);

                using var workbook = new XLWorkbook(excelPath);
                var worksheet = workbook.Worksheet(1);


                var lastRow = worksheet.LastRowUsed();
                var newRow = (lastRow != null) ? lastRow.RowNumber() + 1 : 1;

                foreach (var kvp in excelRow)
                {
                    worksheet.Cell(newRow, kvp.Key).Value = kvp.Value;
                }

                workbook.Save();

                if (!string.IsNullOrWhiteSpace(ReservationNumberBox.Text))
                {
                    var fields = new Dictionary<string, string>
                    {
                        ["Name"] = excelRow[2],
                        ["Date"] = CreatedOnBox.Text.Trim()
                    };
                    var safeName = FileNameUtils.SanitizeFileName(excelRow[2]);

                    var docxPath = Path.Combine(accountFolder, $"Agreement - {safeName}.docx");
                    var resourceName = $"Reports.{toggle}_agreement.docx";
                    await DocxTemplateGenerator.GenerateFromEmbeddedAsync(
                        embeddedResourceName: resourceName,
                        outputPath: docxPath,
                        tokens: fields);
                    DocxTemplateGenerator.OpenInShell(docxPath);
                }

                await _phones.InsertAsync(excelRow[4]);

                ShowFirstPage();
                await Overlay.ShowAsync(true, $"שורה נוספה לקובץ {excelPath}", 4000);
            }
            catch (Exception ex)
            {
                await Overlay.ShowAsync(false, ex.Message);
            }
            finally
            {
                await Loading.HideAsync();
            }
        }
        static void OpenDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            Directory.CreateDirectory(path); // ensures it exists

            Process.Start(new ProcessStartInfo
            {
                FileName = path,          // folder path
                UseShellExecute = true    // required to open folder via shell
            });
        }
        private void ShowFirstPage()
        {
            InputPanel.Visibility = Visibility.Visible;
            TogglePanel.Visibility = Visibility.Visible;
            DataPanel.Visibility  = Visibility.Collapsed;
        }
        private void ShowSecondPage()
        {
            InputPanel.Visibility = Visibility.Collapsed;
            TogglePanel.Visibility = Visibility.Collapsed;
            DataPanel.Visibility  = Visibility.Visible;
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            ShowFirstPage();
        }
        async Task DownloadIfExistsAsync(string url, string targetFolder, string newNameWithoutExt)
        {
            var http = new HttpClient();
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return;

            Directory.CreateDirectory(targetFolder);

            using var resp = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            var mediaType = resp.Content.Headers.ContentType?.MediaType;
            var ext = GetExtensionFromContentType(mediaType);

            // If server doesn't provide a helpful Content-Type, optionally sniff bytes.
            if (string.IsNullOrWhiteSpace(ext))
            {
                var bytes = await resp.Content.ReadAsByteArrayAsync();
                ext = SniffExtension(bytes) ?? "";
                var fallbackPath = Path.Combine(targetFolder, newNameWithoutExt + ext);
                await File.WriteAllBytesAsync(fallbackPath, bytes);
                return;
            }

            var targetPath = Path.Combine(targetFolder, newNameWithoutExt + ext);

            await using var fs = File.Create(targetPath);
            await resp.Content.CopyToAsync(fs);
        }

        static string GetExtensionFromContentType(string? mediaType) => mediaType?.ToLowerInvariant() switch
        {
            "application/pdf" => ".pdf",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",

            // Apple HEIF/HEIC
            "image/heic" => ".heic",
            "image/heif" => ".heif",
            "image/heic-sequence" => ".heic",
            "image/heif-sequence" => ".heif",

            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            _ => ""
        };

        static string? SniffExtension(byte[] bytes)
        {
            if (bytes.Length < 12) return null;

            // PDF: %PDF
            if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46) return ".pdf";

            // JPG: FF D8 FF
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return ".jpg";

            // PNG signature
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return ".png";

            // WEBP: "RIFF" .... "WEBP"
            if (bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
                bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
                return ".webp";

            // HEIC/HEIF are ISO BMFF: bytes[4..8] == "ftyp", and brand at [8..12]
            if (bytes[4] == (byte)'f' && bytes[5] == (byte)'t' && bytes[6] == (byte)'y' && bytes[7] == (byte)'p')
            {
                var brand = System.Text.Encoding.ASCII.GetString(bytes, 8, 4).ToLowerInvariant();
                if (brand is "heic" or "heix" or "hevc" or "hevx") return ".heic";
                if (brand is "heif" or "mif1" or "msf1") return ".heif";
            }

            return null;
        }

    }
}