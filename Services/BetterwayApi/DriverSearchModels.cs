

using Reports.Services.BetterwayApi;

public sealed record BetterwayDriver(
    int Id,
    string Name,
    string? PhoneNumber,
    string? IdNumber,
    string? LicenseNumber,
    string? Email,
    string? Street,
    string? HouseNumber,
    string? City,
    string? ZipCode);
public sealed record DriverSearchHit(
    BetterwayProfile Profile,
    BetterwayDriver Driver);

public sealed record DriverSearchResult(
    BetterwayDriver? FirstMatch,
    IReadOnlyList<BetterwayProfile> ProfilesWithMatch,
    IReadOnlyList<DriverSearchHit> AllHits);
public sealed record DriverSearchRequest(
    string SearchTerm,
    int PageSize = 20,
    int PageNumber = 1,
    string SortedBy = "DateTaken",
    bool IsAscending = false,
    int ResultType = 1);

internal sealed record DriverSearchResponse(
    string? SearchTerm,
    List<BetterwayDriver>? Items,
    int TotalResults,
    int TotalPages,
    int CurrentPage);
    