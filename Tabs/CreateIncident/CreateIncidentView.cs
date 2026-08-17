using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using Reports.Services.Crm;

namespace Reports.Tabs.CreateIncident;

public sealed partial class CreateIncidentView : INotifyPropertyChanged
{
    private string _url = string.Empty;
    private string _data = string.Empty;
    private string _reportAddress = string.Empty;
    private string _reportNumber = string.Empty;
    private string _reportReason = string.Empty;
    private int _reportCost = 0;
    private string _municipality = string.Empty;
    private string _description = string.Empty;
    private string _carLicense = string.Empty;
    private string _vehicleId = string.Empty;
    private string _reservationNumber = string.Empty;
    private string _executionDate = string.Empty;
    private string _accountId = string.Empty;
    private string _serviceType = string.Empty;
    

    private Visibility _inputPanelVisibility = Visibility.Visible;
    private Visibility _dataPanelVisibility = Visibility.Collapsed;

    public string Url { get => _url; set => SetField(ref _url, value); }
    public string Data { get => _data; set => SetField(ref _data, value); }
    public string CarLicense { get => _carLicense; set => SetField(ref _carLicense, value); }
    public string VehicleId { get => _vehicleId; set => SetField(ref _vehicleId, value); }
    public string ReservationNumber { get => _reservationNumber; set => SetField(ref _reservationNumber, value); }
    public string ReportAddress { get => _reportAddress; set => SetField(ref _reportAddress, value); }
    public string ReportNumber { get => _reportNumber; set => SetField(ref _reportNumber, value); }
    public string ReportReason { get => _reportReason; set => SetField(ref _reportReason, value); }
    public int ReportCost { get => _reportCost; set => SetField(ref _reportCost, value); }
    public string Description { get => _description; set => SetField(ref _description, value); }
    public string ExecutionDate { get => _executionDate; set => SetField(ref _executionDate, value); }
    public string AccountId { get => _accountId; set => SetField(ref _accountId, value); }
    public string ServiceType { get => _serviceType; set => SetField(ref _serviceType, value); }
    public string Municipality
    {
        get => _municipality;
        set
        {
            if (SetField(ref _municipality, value))
                OnPropertyChanged(nameof(ReportReasonNames));   // refresh the combo's list
        }
    }

    public IEnumerable<string> ReportReasonNames =>
        (Municipality.Contains("תל אביב")
            ? TelAvivReportReasonByName
            : OthersReportReasonByName2)
        .Keys;

    public Visibility InputPanelVisibility
    {
        get => _inputPanelVisibility;
        set => SetField(ref _inputPanelVisibility, value);
    }

    public Visibility DataPanelVisibility
    {
        get => _dataPanelVisibility;
        set => SetField(ref _dataPanelVisibility, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ShowInput()
    {
        InputPanelVisibility = Visibility.Visible;
        DataPanelVisibility = Visibility.Collapsed;
    }

    public void ShowData()
    {
        InputPanelVisibility = Visibility.Collapsed;
        DataPanelVisibility = Visibility.Visible;
    }
    

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
    public sealed class IncidentRequestData 
    {
        public required string Url { get; init; }
        public required string Data { get; init; }
    }
    public IncidentRequestData ToDraftRequest()
    {
        return new IncidentRequestData
        {
            Url = Url.Trim(),
            Data = Data.Trim()
        };
    }
    public void FillFromDraft(ParkingFinePayload draft)
    {
        AccountId = draft.AccountId;
        ExecutionDate = draft.ExecutionDate;
        CarLicense = draft.VehiclePlateNumber;
        VehicleId = draft.VehicleId;
        ReportNumber = draft.ReportNumber;
        ReportAddress = draft.ReportAddress;
        Municipality = draft.Municipality;
        ReportReason = draft.ReportReason;
        ReportCost = draft.ReportCost;
        Description = draft.Description;
    }
    public ParkingFinePayload ToSubmission()
    {
        var cityId = Municipality.Contains("תל אביב") ? "2be8fb17-f8aa-ed11-aad0-6045bd895af9": "4ce8fb17-f8aa-ed11-aad0-6045bd895af9";
        int? store = ServiceType switch
        {
            "colmobil" => 962940000,
            "lease" => 962940002,
            _ => null
        };
        
        return new ParkingFinePayload
        (
        ExecutionDate: ExecutionDate,
        VehiclePlateNumber: CarLicense,
        ReservationNumber: ReservationNumber,
        VehicleId: VehicleId,
        ReportNumber: ReportNumber,
        ReportAddress: ReportAddress,
        Municipality: Municipality,
        ReportReason: ResolveReportReasonId(),
        ReportCost: ReportCost,
        AccountId: AccountId,
        Description: Description,
        CityId: cityId,
        Store: store,
        ServiceType: ServiceType
        );
    }
    
    private string? ResolveReportReasonId()
    {
        var dict = Municipality.Contains("תל אביב") ? TelAvivReportReasonByName : OthersReportReasonByName2;
        var typed = NormalizeReason(ReportReason);
        if (typed.Length == 0) return null;

        string? bestKey = null;
        double bestScore = 0;

        foreach (var kv in dict)
        {
            if (kv.Key.Length == 0) continue;
            var score = Similarity(typed, NormalizeReason(kv.Key));
            if (score > bestScore) { bestScore = score; bestKey = kv.Key; }
        }

        if (bestKey is null || bestScore < 0.6) return null;

        ReportReason = bestKey;          // snap the box to the matched name so you SEE what got picked
        return dict[bestKey];
    }

    private static string NormalizeReason(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim();
        // drop leading enumerator like "א. " / "ב. "
        if (s.Length > 2 && s[1] == '.' ) s = s[2..].TrimStart();
        // drop trailing "(א)" variant marker
        if (s.EndsWith("(א)")) s = s[..^3].TrimEnd();
        // collapse internal whitespace
        return string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static double Similarity(string a, string b)
    {
        if (a == b) return 1.0;
        // strong signal: one contains the other (your case — typed is a substring of the key)
        if (a.Contains(b) || b.Contains(a)) return 0.95;

        // fall back to Levenshtein ratio
        int dist = Levenshtein(a, b);
        int max = Math.Max(a.Length, b.Length);
        return max == 0 ? 0 : 1.0 - (double)dist / max;
    }

    private static int Levenshtein(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        return d[a.Length, b.Length];
    }
    public void Reset()
    {
        var props = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var p in props)
        {
            var defaultValue = p.PropertyType.IsValueType
                ? Activator.CreateInstance(p.PropertyType)
                : null;
            p.SetValue(this, defaultValue);
        }
    }
    
    private static readonly Dictionary<string, string?> TelAvivReportReasonByName = new(StringComparer.Ordinal)
    {
        [""] = null,
        ["א. השתמשת שלא כדין בנתיב נסיעה אשר יועד וסומן בתמרור כנתיב תחבורה ציבורית"] = "94d99b28-ddd0-ea11-a812-000d3aafe1cf",
        ["החנית את רכבך במקום שהחניה בו נאסרה והאיסור מסומן בתמרור"] = "2be2af5f-ddd0-ea11-a812-000d3aafe1cf",
        ["במקום שהחניה בו נאסרה"] = "2be2af5f-ddd0-ea11-a812-000d3aafe1cf",
        ["הכנסה, נהיגה, השארה של רכב בגן ללא היתר מראש העיריה"] = "4745a881-5908-ee11-8f6e-000d3aad35f2",
        ["העמדת את הרכב במקום חניה לרכב מורשה"] = "139f3535-ddd0-ea11-a812-000d3aafe1cf",
        ["העמדת/ החנית את הרכב במקום בו החניה אסורה על פי תמרור אין עצירה"] = "b74ea847-dcd0-ea11-a812-000d3aafe1cf",
        ["העמדת/ החנית את הרכב במקום המתיר חניה לכלי רכב מסוימים ובשעות מסוימות"] = "5e67b041-ddd0-ea11-a812-000d3aafe1cf",
        ["העמדת/ החנית את הרכב במקום חניה המיועד לרכב של נכה"] = "3633da7d-ddd0-ea11-a812-000d3aafe1cf",
        ["העמדת/ החנית את הרכב בנתיב שיועד לתחבורה ציבורית ובתחום תחנת אוטובוסים"] = "0b01b44d-ddd0-ea11-a812-000d3aafe1cf",
        ["העמדת/ החנית את הרכב מבלי ששילמת אגרת חניה"] = "585d20a2-ded0-ea11-a812-000d3aafe1cf",
        ["העמדת/ החנית את רכבך במקום חניה והוא אינו שייך לסוג הרכב שחנייתו שם הותרה"] = "25c0e06b-ddd0-ea11-a812-000d3aafe1cf",
        ["העמדת/ החנית את רכבך בתחום שנים עשר מטר מהצומת"] = "ef1c5646-ded0-ea11-a812-000d3aafe1cf",
        ["העמדת/ החנית את רכבך הנ\"ל לצד רכב אחר שעמד לצד הדרך (חניה כפולה)"] = "c0db2534-ded0-ea11-a812-000d3aafe1cf",
        ["העמדת/ החנית את רכבך הנ\"ל תוך הפרעה או עיכוב התנועה"] = "f6299609-ded0-ea11-a812-000d3aafe1cf",
        ["באופן שיש בו כדי להפריע"] = "f6299609-ded0-ea11-a812-000d3aafe1cf",
        ["העמדת/ החנית את רכבך/ חלק מרכבך על המדרכה"] = "efbea515-ded0-ea11-a812-000d3aafe1cf",
        ["העמדת/ החנית את רכבך/חלק מרכבך על המדרכה- 2 גלגלים"] = "78cfbe21-ded0-ea11-a812-000d3aafe1cf",
        ["העמדת/החנית את הרכב באופן העלול להפריע או לעכב את התנועה"] = "98ae6df4-f1b1-ed11-9885-6045bd8c9d7a",
        ["העמדת/החנית את הרכב במקום בו החניה אסורה על פי תמרור אין עצירה"] = "80ae6df4-f1b1-ed11-9885-6045bd8c9d7a",
        ["העמדת/החנית את הרכב במקום בו החניה אסורה על פי תמרור אין עצירה פרט לפריקה וטעינה מיידית ובלתי פוסקת"] = "94ae6df4-f1b1-ed11-9885-6045bd8c9d7a",
        ["העמדת/החנית את הרכב במקום חניה מוסדר והשתמשת באמצעי תשלום שלא בהתאם להוראות החניה"] = "697b3b5a-3b60-ee11-8df0-0022487fe0e0",
        ["העמדת/החנית את הרכב במקום חניה מוסדר מבלי ששילמת אגרת הסדר חניה באמצעי תשלום שנקבע בחוק העזר"] = "fa947315-df9f-ed11-aad1-0022487fee57",
    };
    private static readonly Dictionary<string, string?> OthersReportReasonByName2 = new(StringComparer.Ordinal)
    {
        [""] = null,
        ["אנא בחר סיבה אחרת"] = "65306c08-7325-ee11-9cbc-6045bd895bb0",
        ["בלימת פתע"] = "d73e8641-1905-ee11-8f6e-6045bd895e74",
        ["החנית את רכבך במקום שהחניה בו נאסרה והאיסור מסומן בתמרור(א)"] = "90a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["במקום שהחניה בו נאסרה"] = "90a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת את הרכב במקום חניה לרכב מורשה(א)"] = "92a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/ החנית את הרכב במקום בו החניה אסורה על פי תמרור אין עצירה(א)"] = "94a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/ החנית את הרכב במקום המתיר חניה לכלי רכב מסוימים ובשעות מסוימות(א)"] = "96a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/ החנית את הרכב במקום חניה המיועד לרכב של נכה(א)"] = "98a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/ החנית את הרכב בנתיב שיועד לתחבורה ציבורית ובתחום תחנת אוטובוסים(א)"] = "9aa94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/החנית את רכבך הנ״ל בתחום תחנת אוטובוסים ו/או נתיב תחבורה ציבורית לפי סעיף 72 (א) (12) לתקנות ה תעבורה"] = "9aa94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/ החנית את הרכב מבלי ששילמת אגרת חניה(א)"] = "9ca94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["במקום חניה מסודר החנית את רכבך בלי ששלמת אגרת הסדר חניה"] = "9ca94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["מבלי ששולמה אגרת החניה במקום חניה מוסדר כאמור בחוק העזר."] = "9ca94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/ החנית את רכבך במקום חניה והוא אינו שייך לסוג הרכב שחנייתו שם הותרה(א)"] = "9ea94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/ החנית את רכבך בתחום שנים עשר מטר מהצומת(א)"] = "a0a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/ החנית את רכבך הנ\"ל לצד רכב אחר שעמד לצד הדרך (חניה כפולה)(א)"] = "a2a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/ החנית את רכבך הנ\"ל תוך הפרעה או עיכוב התנועה(א)"] = "a4a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["באופן שיש בו כדי להפריע"] = "a4a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/ החנית את רכבך/ חלק מרכבך על המדרכה(א)"] = "a6a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/ החנית את רכבך/חלק מרכבך על המדרכה- 2 גלגלים(א)"] = "a8a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["החנית את רכבך עם 2 גלגלים בלבד על המדרכה"] = "a8a94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/החנית את הרכב במקום חניה מוסדר מבלי ששילמת אגרת הסדר חניה באמצעי תשלום שנקבע בחוק העזר(א)"] = "aaa94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["העמדת/החנית את רכבך הנ\"ל במקום חניה שלא בתוך אחד השטחים המסומנים בקווי צבע, או באופן אחר. בניגוד לסעיף 7(א) בחוק העזר"] = "ccaa3cf1-4f73-ef11-a670-000d3a4cdf46",
        ["העמיד/החנה/עצר/ השאיר רכב במקום הגורם הפרעה/עיכוב התנועה"] = "429dcae7-e773-ee11-8179-6045bd895a41",
        ["השלכת פסולת ברשות רבים/יחיד"] = "fba75caf-b90d-ee11-8f6d-0022487fe31a",
        ["חניה באדום לבן(א)"] = "aca94c7c-1cab-ed11-aad0-6045bd8c985b",
        ["חניה בשטח של גן ציבורי/ שפת הים"] = "34ee6c9e-3213-ee11-8f6d-6045bd895a41",
        ["חניה כפולה"] = "bdd2ed97-69f5-ed11-8849-6045bd895a41",
        ["מהירות מ 31 עד 40 קמ\"ש מעל, דרך בין עירונית"] = "e060d880-9cd6-f011-8544-7c1e5229d764",
    };

}