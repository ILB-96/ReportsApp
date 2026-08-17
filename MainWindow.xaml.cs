using Wpf.Ui.Abstractions;

namespace Reports;

public partial class MainWindow
{
    public MainWindow(INavigationViewPageProvider pageProvider)
    {
        InitializeComponent();

        RootNavigation.SetPageProviderService(pageProvider);
        

        Loaded += (_, _) =>
        {
            RootNavigation.Navigate(typeof(Tabs.SignatureForm));
        };
    }
}