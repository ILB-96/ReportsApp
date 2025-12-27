using System.Windows;


namespace Reports
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            
            Loaded += (_, _) => RootNavigation.Navigate(typeof(SignatureForm));
        }
    }
}
