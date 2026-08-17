namespace Reports.Services.BetterwayApi;

public sealed record VehicleSearchRequest(
    string SearchTerm,
    int PageSize = 20,
    int PageNumber = 1,
    string SortedBy = "Owner",
    bool IsAscending = true,
    int ResultType = 1);

public sealed class VehicleSearchResponse
{
    public string? SearchTerm { get; set; }
    public List<VehicleItem>? Items { get; set; }
    public int TotalResults { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}

public sealed class VehicleItem
{
    public int Id { get; set; }
    public string? PlateNumber { get; set; }
    public string? Model { get; set; }
    public string? Ownership { get; set; }
    public DateTime? StartDate { get; set; }   // contract start (item-level)
    public DateTime? EndDate { get; set; }     // contract end
    public VehicleOwner? Owner { get; set; }
    public ContractProfile? ContractProfile { get; set; }
}

public sealed class VehicleOwner
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class ContractProfile
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public sealed record VehicleLookupResult(
    int Id,
    string PlateNumber,
    bool HasContract,
    DateTime? ContractStartDate,
    DateTime? ContractEndDate,
    DateTime? OwnershipStartDate,
    DateTime? OwnershipEndDate);