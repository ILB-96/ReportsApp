using System;

namespace Reports.Services.Email;

public sealed class OutlookEmailComposerService : IEmailComposerService
{
    public void OpenDraft(EmailDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var outlookType = Type.GetTypeFromProgID("Outlook.Application");
        if (outlookType is null)
            throw new InvalidOperationException("Outlook is not installed on this computer.");

        dynamic? outlookApp = Activator.CreateInstance(outlookType);
        if (outlookApp is null)
            throw new InvalidOperationException("Failed to start Outlook.");

        dynamic mail = outlookApp.CreateItem(0); // olMailItem

        mail.To = draft.To;
        mail.CC = draft.Cc ?? string.Empty;
        mail.BCC = draft.Bcc ?? string.Empty;
        mail.Subject = draft.Subject;

        mail.Display();

        var existingHtml = mail.HTMLBody as string ?? string.Empty;
        mail.HTMLBody = draft.HtmlBody + existingHtml;
    }
}