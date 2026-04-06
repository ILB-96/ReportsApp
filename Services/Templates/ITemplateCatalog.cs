namespace Reports.Services.Templates;

public interface ITemplateCatalog
{
    string AgreementTemplate(string brand);
    string ReservationTemplate(string brand);
    string SignatureTemplate(string brand);
}