using System.Threading;
using System.Threading.Tasks;

namespace Reports.Services.Drivers;

public interface IDriverSubmissionService
{
    Task<DriverSubmissionResult> SubmitAsync(DriverSubmission submission, CancellationToken ct = default);
}