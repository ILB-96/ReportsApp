namespace Reports.Services.Email;

public sealed record EmailDraft(
    string To,
    string Subject,
    string PlainBody,
    string HtmlBody,
    string? Cc = null,
    string? Bcc = null);

public interface IEmailDraftBuilder<in TModel>
{
    EmailDraft Build(TModel model);
}

public interface IEmailComposerService
{
    void OpenDraft(EmailDraft draft);
}