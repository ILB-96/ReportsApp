namespace Reports.Services.Drivers;

public interface IDriverDraftService
{
    Task<CreateDriverDraft> LoadDraftAsync(CreateDriverRequest request, CancellationToken ct = default);
}