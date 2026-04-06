using System;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace Reports.Services.Email;

public sealed class EmailComposerService : IEmailComposerService
{
    public void OpenDraft(EmailDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (string.IsNullOrWhiteSpace(draft.To))
            throw new ArgumentException("Recipient is required.", nameof(draft));

        if (string.IsNullOrWhiteSpace(draft.Subject))
            throw new ArgumentException("Subject is required.", nameof(draft));

        CopyEmailBodyToClipboard(draft.HtmlBody, draft.PlainBody);

        var mailtoUri =
            $"mailto:{Uri.EscapeDataString(draft.To)}" +
            $"?subject={Uri.EscapeDataString(draft.Subject)}";

        Process.Start(new ProcessStartInfo
        {
            FileName = mailtoUri,
            UseShellExecute = true
        });
    }

    private static void CopyEmailBodyToClipboard(string htmlBody, string plainBody)
    {
        var data = new DataObject();

        data.SetData(DataFormats.Html, BuildClipboardHtml(htmlBody ?? string.Empty));
        data.SetData(DataFormats.UnicodeText, plainBody ?? string.Empty);
        data.SetData(DataFormats.Text, plainBody ?? string.Empty);

        Clipboard.SetDataObject(data, true);
    }

    private static string BuildClipboardHtml(string htmlFragment)
    {
        const string startFragment = "<!--StartFragment-->";
        const string endFragment = "<!--EndFragment-->";

        var html =
            "<html><body>" +
            startFragment +
            htmlFragment +
            endFragment +
            "</body></html>";

        const string headerTemplate =
            "Version:1.0\r\n" +
            "StartHTML:{0:D10}\r\n" +
            "EndHTML:{1:D10}\r\n" +
            "StartFragment:{2:D10}\r\n" +
            "EndFragment:{3:D10}\r\n";

        var dummyHeader = string.Format(headerTemplate, 0, 0, 0, 0);
        var encoding = Encoding.UTF8;

        var startHtml = encoding.GetByteCount(dummyHeader);

        var startFragmentOffsetInHtml =
            encoding.GetByteCount(html[..(html.IndexOf(startFragment, StringComparison.Ordinal) + startFragment.Length)]);

        var endFragmentOffsetInHtml =
            encoding.GetByteCount(html[..html.IndexOf(endFragment, StringComparison.Ordinal)]);

        var endHtml = startHtml + encoding.GetByteCount(html);
        var startFragmentIndex = startHtml + startFragmentOffsetInHtml;
        var endFragmentIndex = startHtml + endFragmentOffsetInHtml;

        var header = string.Format(
            headerTemplate,
            startHtml,
            endHtml,
            startFragmentIndex,
            endFragmentIndex);

        return header + html;
    }
}