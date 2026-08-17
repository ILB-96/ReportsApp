// Request body — mirror your DriverSearchRequest. If the endpoint wants a
// different property name (Query, Text, etc.), rename here.

namespace Reports.Services.BetterwayApi;

public sealed record ClientSearchRequest(
    string SearchTerm,
    int PageSize = 20,
    int PageNumber = 1,
    bool IsAscending = true,
    int ResultType = 12);
// Response envelope — mirrors DriverSearchResponse (.Items). If Clients/Search
// returns a bare array, or wraps under Data/Results, or is paged
// { Items, TotalCount }, adjust to match.
public sealed record ClientSearchResponse(List<BetterwayClient> Items);

// The client model — fill in the actual fields the payload returns.
public sealed record BetterwayClient(
    int ClientId,
    string IdNumber,
    bool HasTransferForm,
    bool HasLawyerTransferForm,
    bool HasClosingPage,
    bool HasOpeningPage,
    bool HasGeneralContractForm,
    bool HasDriversLicense,
    DateTime UpdateDate,
    string Name,
    bool IsForeignCitizen,
    string Email,
    decimal? Fee,
    bool AllowsApproval,
    int? InternalClientId,
    bool AllowContractHtmlAggregation,
    bool ForbidTransfer,
    string Street,
    string HouseNumber,
    string City,
    string ZipCode);

public sealed record ClientSearchHit(BetterwayProfile Profile, BetterwayClient Client);

public sealed record ClientSearchResult(
    BetterwayClient? FirstMatch,
    IReadOnlyList<BetterwayProfile> ProfilesWithMatch,
    IReadOnlyList<ClientSearchHit> AllHits);