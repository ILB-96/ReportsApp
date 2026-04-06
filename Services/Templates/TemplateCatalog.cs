using Microsoft.Extensions.Options;
using Reports.Configuration;

namespace Reports.Services.Templates;

public sealed class TemplateCatalog : ITemplateCatalog
{
    private readonly AppOptions _options;

    public TemplateCatalog(IOptions<AppOptions> options)
    {
        _options = options.Value;
    }

    public string AgreementTemplate(string brand)
        => _options.AgreementPath.Replace("{brand}", brand);
    
    public string ReservationTemplate(string brand)
        => _options.ReservationPath.Replace("{brand}", brand);

    public string SignatureTemplate(string brand)
        => _options.SignaturePath.Replace("{brand}", brand);
}