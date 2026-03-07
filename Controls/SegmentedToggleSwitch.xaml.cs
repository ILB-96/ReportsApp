using System.Windows.Controls;
using System.Windows;

namespace Reports.Controls
{
    public partial class SegmentedToggleSwitch : UserControl
    {
        public SegmentedToggleSwitch() => InitializeComponent();

        public static readonly DependencyProperty LeftTextProperty =
            DependencyProperty.Register(nameof(LeftText), typeof(string), typeof(SegmentedToggleSwitch),
                new PropertyMetadata(string.Empty));

        public string LeftText
        {
            get => (string)GetValue(LeftTextProperty);
            set => SetValue(LeftTextProperty, value);
        }

        public static readonly DependencyProperty RightTextProperty =
            DependencyProperty.Register(nameof(RightText), typeof(string), typeof(SegmentedToggleSwitch),
                new PropertyMetadata(string.Empty));

        public string RightText
        {
            get => (string)GetValue(RightTextProperty);
            set => SetValue(RightTextProperty, value);
        }

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(SegmentedToggleSwitch),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public static readonly DependencyProperty SwitchWidthProperty =
            DependencyProperty.Register(nameof(SwitchWidth), typeof(double), typeof(SegmentedToggleSwitch),
                new PropertyMetadata(50d));

        public double SwitchWidth
        {
            get => (double)GetValue(SwitchWidthProperty);
            set => SetValue(SwitchWidthProperty, value);
        }

        public static readonly DependencyProperty SwitchHeightProperty =
            DependencyProperty.Register(nameof(SwitchHeight), typeof(double), typeof(SegmentedToggleSwitch),
                new PropertyMetadata(28d));

        public double SwitchHeight
        {
            get => (double)GetValue(SwitchHeightProperty);
            set => SetValue(SwitchHeightProperty, value);
        }

        public static readonly DependencyProperty OuterMarginProperty =
            DependencyProperty.Register(nameof(OuterMargin), typeof(Thickness), typeof(SegmentedToggleSwitch),
                new PropertyMetadata(new Thickness(0, 16, 0, 16)));

        public Thickness OuterMargin
        {
            get => (Thickness)GetValue(OuterMarginProperty);
            set => SetValue(OuterMarginProperty, value);
        }

        public static readonly DependencyProperty LeftMarginProperty =
            DependencyProperty.Register(nameof(LeftMargin), typeof(Thickness), typeof(SegmentedToggleSwitch),
                new PropertyMetadata(new Thickness(0, 0, 10, 0)));

        public Thickness LeftMargin
        {
            get => (Thickness)GetValue(LeftMarginProperty);
            set => SetValue(LeftMarginProperty, value);
        }

        public static readonly DependencyProperty RightMarginProperty =
            DependencyProperty.Register(nameof(RightMargin), typeof(Thickness), typeof(SegmentedToggleSwitch),
                new PropertyMetadata(new Thickness(10, 0, 0, 0)));

        public Thickness RightMargin
        {
            get => (Thickness)GetValue(RightMarginProperty);
            set => SetValue(RightMarginProperty, value);
        }
    }
}