using System.IO;
using Reports.Services.Crm;

namespace Reports.Services.Agreement;

public interface IAgreementSubmissionService
{
    Task SubmitAsync(DriverAgreementData submission, CancellationToken ct = default);
}

public sealed class AgreementSubmissionService(IAgreementGenerator agreementGenerator)
    : IAgreementSubmissionService
{
    public Task SubmitAsync(DriverAgreementData submission, CancellationToken ct = default)
    {
        var downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        return agreementGenerator.GenerateAsync(
            submission.FullName,
            submission.CreatedOn,
            submission.Brand,
            downloadsPath,
            ct);
    }
}