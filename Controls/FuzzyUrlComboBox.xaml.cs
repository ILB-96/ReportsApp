using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Reports.Controls;

public partial class FuzzyUrlComboBox : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(FuzzyUrlComboBox),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(FuzzyUrlComboBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private bool _openedByUser;

    public FuzzyUrlComboBox()
    {
        InitializeComponent();

        // If the list changes while open, keep it open and stable (no toggling).
        Combo.DropDownOpened += (_, _) => _openedByUser = true;
        Combo.DropDownClosed += (_, _) => _openedByUser = false;
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private void Combo_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Open no matter where you click on the field.
        if (!Combo.IsDropDownOpen)
        {
            Combo.IsDropDownOpen = true;
            _openedByUser = true;
        }
    }

    private void Combo_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // Open when tabbing into it, but only if it was user focus (not programmatic).
        if (!Combo.IsDropDownOpen)
        {
            Combo.IsDropDownOpen = true;
            _openedByUser = true;
        }
    }
}