using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using Reports.Services.Crm;

namespace Reports.Tabs.CreateAgreement;

public sealed partial class CreateAgreementView : INotifyPropertyChanged
{
    private string _url       = string.Empty;
    private string _fullName  = string.Empty;
    private string _createdOn = string.Empty;
    

    private Visibility _inputPanelVisibility = Visibility.Visible;
    private Visibility _dataPanelVisibility  = Visibility.Collapsed;

    public string Url       { get => _url; set => SetField(ref _url, value); }
    public string FullName  { get => _fullName; set => SetField(ref _fullName, value); }
    public string CreatedOn { get => _createdOn; set => SetField(ref _createdOn, value); }

    public Visibility InputPanelVisibility
    {
        get => _inputPanelVisibility;
        set => SetField(ref _inputPanelVisibility, value);
    }

    public Visibility DataPanelVisibility
    {
        get => _dataPanelVisibility;
        set => SetField(ref _dataPanelVisibility, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ShowInput()
    {
        InputPanelVisibility = Visibility.Visible;
        DataPanelVisibility  = Visibility.Collapsed;
    }

    public void ShowData()
    {
        InputPanelVisibility = Visibility.Collapsed;
        DataPanelVisibility  = Visibility.Visible;
    }
    

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
    public sealed class AgreementRequestData 
    {
        public required string Url { get; init; }
    }
    public AgreementRequestData ToDraftRequest()
    {
        return new AgreementRequestData
        {
            Url = Url.Trim(),
        };
    }
    public void FillFromDraft(DriverAgreementData draft)
    {
        FullName  = draft.FullName;
        CreatedOn = draft.CreatedOn;
    }
    public DriverAgreementData ToSubmission(string brand)
    {
        
        return new DriverAgreementData
        (
            Brand: brand,
        FullName: FullName,
        CreatedOn: CreatedOn
        );
    }

}