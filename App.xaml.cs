using System.Configuration;
using System.Data;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Reports;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplicationThemeManager.Apply(
            ApplicationTheme.Light,   // or Dark
            WindowBackdropType.Mica,  // or Tabbed, Acrylic, etc.
            true                      // force update existing windows
        );
    }
}