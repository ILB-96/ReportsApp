using System.Net;
using System.Text;
using Reports.Services.Email;

namespace Reports.Services.Email.CustomerRequests;

public sealed class CustomerRequestEmailDraftBuilder : IEmailDraftBuilder<CustomerRequestEmailModel>
{
    private const string To = "info@betterway.co.il";
    private const string Rlm = "\u200F";

    public EmailDraft Build(CustomerRequestEmailModel model)
    {
        var subject = $"הקמת לקוח - {model.Company} - {model.IdNumber}";
        var plain = BuildPlainTextBody(model);
        var html = BuildHtmlBody(model);

        return new EmailDraft(To, subject, plain, html);
    }

    private static string BuildPlainTextBody(CustomerRequestEmailModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{Rlm}היי,");
        sb.AppendLine();
        sb.AppendLine($"{Rlm}ח.פ חברה: {model.Company}");
        sb.AppendLine($"{Rlm}שם מלא: {model.FullName}");
        sb.AppendLine($"{Rlm}תעודת זהות: {model.IdNumber}");
        sb.AppendLine($"{Rlm}כתובת מייל: {model.Email}");
        sb.AppendLine($"{Rlm}עיר: {model.Address.City}");
        sb.AppendLine($"{Rlm}רחוב: {model.Address.Street}");
        sb.AppendLine($"{Rlm}מספר רחוב: {model.Address.StreetNumber}");
        sb.AppendLine($"{Rlm}מספר דירה: {model.Address.ApartmentNumber}");
        sb.AppendLine($"{Rlm}מיקוד: {model.Address.ZipCode}");
        sb.AppendLine();
        sb.AppendLine($"{Rlm}מספר רכב לשיוך: {model.CarNumber}");
        sb.AppendLine($"{Rlm}תאריך ושעת התחלה: {model.StartTime}");
        sb.AppendLine($"{Rlm}תאריך ושעת סיום: {model.EndTime}");

        return sb.ToString();
    }

    private static string BuildHtmlBody(CustomerRequestEmailModel model)
    {
        return $@"
<div dir='rtl' style='font-family: Arial, sans-serif; font-size: 14.5px; text-align: right; line-height: 1.6; margin: 0;'>
  <p style='margin: 0 0 12px 0;'>היי,</p>
  <p style='margin: 0;'>
    ח.פ חברה: {Html(model.Company)}<br>
    שם מלא: {Html(model.FullName)}<br>
    תעודת זהות: {Html(model.IdNumber)}<br>
    כתובת מייל: {Html(model.Email)}<br>
    עיר: {Html(model.Address.City)}<br>
    רחוב: {Html(model.Address.Street)}<br>
    מספר רחוב: {Html(model.Address.StreetNumber)}<br>
    מספר דירה: {Html(model.Address.ApartmentNumber)}<br>
    מיקוד: {Html(model.Address.ZipCode)}<br><br>
    מספר רכב לשיוך: {Html(model.CarNumber)}<br>
    תאריך ושעת התחלה: {Html(model.StartTime)}<br>
    תאריך ושעת סיום: {Html(model.EndTime)}
  </p>
</div>";
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}