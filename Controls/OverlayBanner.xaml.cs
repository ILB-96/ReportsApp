using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Reports.Controls
{
    public partial class OverlayBanner : UserControl
    {
        public OverlayBanner()
        {
            InitializeComponent();
        }

        public async Task ShowAsync(
            bool success,
            string text,
            int milliseconds = 2500)
        {
            var successBrush = TryFindResource("SystemFillColorSuccessBrush") as Brush
                               ?? new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));

            var dangerBrush = TryFindResource("SystemFillColorCriticalBrush") as Brush
                              ?? new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));

            var textBrush = TryFindResource("TextOnAccentFillColorPrimaryBrush") as Brush
                            ?? Brushes.White;

            Banner.Background = success ? successBrush : dangerBrush;
            Icon.Foreground = textBrush;
            Message.Foreground = textBrush;

            Icon.Text = success ? "✔" : "✖";
            Message.Text = text;

            Banner.Visibility = Visibility.Visible;

            (Resources["ShowStoryboard"] as Storyboard)?.Begin();
            await Task.Delay(milliseconds);

            (Resources["HideStoryboard"] as Storyboard)?.Begin();
            await Task.Delay(220);

            Banner.Visibility = Visibility.Collapsed;
        }
    }
}