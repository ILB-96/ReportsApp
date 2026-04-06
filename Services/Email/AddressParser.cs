using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Reports.Services.Email;

public sealed record AddressInfo(
    string Street,
    string StreetNumber,
    string ApartmentNumber,
    string City,
    string ZipCode);

public interface IAddressParser
{
    AddressInfo Parse(string? input);
}

public sealed partial class AddressParser : IAddressParser
{
    public AddressInfo Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Empty();

        var parts = input.Split(',')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        var street = string.Empty;
        var streetNumber = string.Empty;
        var apartmentNumber = string.Empty;
        var city = string.Empty;
        var zipCode = string.Empty;

        var index = 0;

        if (index < parts.Length)
        {
            var match = StreetAndNumberRegex().Match(parts[index]);
            if (match.Success)
            {
                street = match.Groups[1].Value.Trim();
                streetNumber = match.Groups[2].Value.Trim();
            }
            else
            {
                street = parts[index];
            }

            index++;
        }

        if (index < parts.Length) apartmentNumber = parts[index++];
        if (index < parts.Length) city = parts[index++];
        if (index < parts.Length) zipCode = parts[index];

        return new AddressInfo(street, streetNumber, apartmentNumber, city, zipCode);
    }

    private static AddressInfo Empty() => new("", "", "", "", "");

    [GeneratedRegex(@"^(.*?)[\s]+(\d+)$")]
    private static partial Regex StreetAndNumberRegex();
}