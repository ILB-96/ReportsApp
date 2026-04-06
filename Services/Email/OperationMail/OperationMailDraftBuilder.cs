using System.Net;
using System.Text;
using Reports.Services.Email;

namespace Reports.Services.Email.OperationMail;

public sealed class OperationMailDraftBuilder : IEmailDraftBuilder<OperationMailModel>
{
    public EmailDraft Build(OperationMailModel model)
    {
        var managerMail = model.Brand == "goto"
            ? "binyamin.reuven@gotoglobal.com"
            : "idan.gur@gotoglobal.com";

        var subject = $"בקשה להסבת דוח תפעול - {model.AccountFullName} - {model.ReportNumber}";
        var plainBody = BuildPlainBody(model);
        var htmlBody = BuildHtmlBody(model);

        return new EmailDraft(
            To: managerMail,
            Subject: subject,
            PlainBody: plainBody,
            HtmlBody: htmlBody);
    }

    private static string BuildPlainBody(OperationMailModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("היי,");
        sb.AppendLine();
        sb.AppendLine($"התקבל דוח בגין {model.ReportReason}");
        sb.AppendLine();
        sb.AppendLine("כל המידע אודות הדוח מופיע כאן למטה:");
        sb.AppendLine($"מספר דוח: {model.ReportNumber}");
        sb.AppendLine($"מספר הרכב: {model.CarLicense}");
        sb.AppendLine($"תאריך ושעה: {model.ReportDate}");
        sb.AppendLine($"כתובת: {model.ReportAddress}");
        sb.AppendLine($"סכום הדוח: {model.ReportPrice}");
        sb.AppendLine($"רשות: {model.ReportCity}");
        sb.AppendLine();
        sb.AppendLine($"לפי הנתונים נראה שהדוח שייך ל{model.AccountFullName}.");

        return sb.ToString();
    }

    private static string BuildHtmlBody(OperationMailModel model)
    {
        return $@"
<html>
  <body style='font-family: Arial, sans-serif; font-size: 14.5px; direction: rtl; text-align: right; line-height: 1.6;'>
    <p style='margin: 0 0 12px 0;'>היי,</p>

    התקבל דוח בגין {Html(model.ReportReason)}
    <br><br>

    <u>כל המידע אודות הדוח מופיע כאן למטה:</u>
    <br>
    מספר דוח: {Html(model.ReportNumber)}
    <br>
    מספר הרכב: {Html(model.CarLicense)}
    <br>
    תאריך ושעה: {Html(model.ReportDate)}
    <br>
    כתובת: {Html(model.ReportAddress)}
    <br>
    סכום הדוח: {Html(model.ReportPrice)}
    <br>
    רשות: {Html(model.ReportCity)}
    <br><br>

    לפי הנתונים נראה שהדוח שייך ל{Html(model.AccountFullName)}.
  </body>
</html>";
    }

    private static string Html(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty);
}