using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Windows;
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