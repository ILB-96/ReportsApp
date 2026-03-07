using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace Reports;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            // Navigate by TYPE (this is what your Wpf.Ui version supports)
            RootNavigation.Navigate(typeof(Tabs.SignatureForm));
        };
    }
}