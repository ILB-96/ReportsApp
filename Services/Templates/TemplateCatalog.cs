using Microsoft.Extensions.Options;
using Reports.Configuration;

namespace Reports.Services.Templates;

public sealed class TemplateCatalog(IOptions<AppOptions> options) : ITemplateCatalog
{
    private readonly AppOptions _options = options.Value;

    public string AgreementTemplate(string brand)
        => _options.AgreementPath.Replace("{brand}", brand);
    
    public string ReservationTemplate(string brand)
        => _options.ReservationPath.Replace("{brand}", brand);

    public string SignatureTemplate(string brand)
        => _options.SignaturePath.Replace("{brand}", brand);
}