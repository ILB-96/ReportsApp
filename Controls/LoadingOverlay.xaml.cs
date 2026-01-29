using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Reports.Controls
{
    public partial class LoadingOverlay : UserControl
    {
        private int _version;
        private long _shownAtTicks;

        public LoadingOverlay()
        {
            InitializeComponent();
            UpdateComputedVisibilities();
        }

        // ---- Public API ----
        public event EventHandler? CancellationRequested;

        public async Task ShowAsync(string message, string? detail = null)
        {
            Message = message;
            Detail = detail;
            IsOpen = true;

            // Let the show animation start before continuing (optional)
            await Task.Yield();
        }

        public async Task HideAsync()
        {
            IsOpen = false;
            await Task.Yield();
        }

        /// <summary>
        /// Helper scope: using(...) shows overlay; dispose hides it (anti-flicker included).
        /// </summary>
        public IDisposable BeginScope(string message, string? detail = null)
        {
            _ = ShowAsync(message, detail);
            var captured = ++_version;
            return new Scope(this, captured);
        }

        private sealed class Scope : IDisposable
        {
            private readonly LoadingOverlay _owner;
            private readonly int _capturedVersion;
            private bool _disposed;

            public Scope(LoadingOverlay owner, int capturedVersion)
            {
                _owner = owner;
                _capturedVersion = capturedVersion;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _ = _owner.HideIfLatestAsync(_capturedVersion);
            }
        }

        private async Task HideIfLatestAsync(int capturedVersion)
        {
            // Only hide if nobody else has shown it since.
            if (capturedVersion != _version) return;

            var minMs = MinimumShowTimeMilliseconds;
            if (minMs > 0 && _shownAtTicks != 0)
            {
                var elapsedMs = (long)((Stopwatch.GetTimestamp() - _shownAtTicks) * 1000.0 / Stopwatch.Frequency);
                var remaining = (int)Math.Max(0, minMs - elapsedMs);
                if (remaining > 0)
                    await Task.Delay(remaining);
            }

            if (capturedVersion != _version) return;
            IsOpen = false;
        }

        // ---- Dependency Properties ----
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(LoadingOverlay),
                new PropertyMetadata(false, OnIsOpenChanged));

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoadingOverlay),
                new PropertyMetadata("טוען..."));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty DetailProperty =
            DependencyProperty.Register(nameof(Detail), typeof(string), typeof(LoadingOverlay),
                new PropertyMetadata(string.Empty, OnDetailChanged));

        public string Detail
        {
            get => (string)GetValue(DetailProperty);
            set => SetValue(DetailProperty, value);
        }

        public static readonly DependencyProperty OverlayBackgroundProperty =
            DependencyProperty.Register(nameof(OverlayBackground), typeof(Brush), typeof(LoadingOverlay),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00))));

        public Brush OverlayBackground
        {
            get => (Brush)GetValue(OverlayBackgroundProperty);
            set => SetValue(OverlayBackgroundProperty, value);
        }

        public static readonly DependencyProperty IsBlockingProperty =
            DependencyProperty.Register(nameof(IsBlocking), typeof(bool), typeof(LoadingOverlay),
                new PropertyMetadata(true));

        /// <summary>When true, overlay blocks clicks under it.</summary>
        public bool IsBlocking
        {
            get => (bool)GetValue(IsBlockingProperty);
            set => SetValue(IsBlockingProperty, value);
        }

        public static readonly DependencyProperty IsCancelableProperty =
            DependencyProperty.Register(nameof(IsCancelable), typeof(bool), typeof(LoadingOverlay),
                new PropertyMetadata(false, OnCancelableChanged));

        public bool IsCancelable
        {
            get => (bool)GetValue(IsCancelableProperty);
            set => SetValue(IsCancelableProperty, value);
        }

        public static readonly DependencyProperty CancelTextProperty =
            DependencyProperty.Register(nameof(CancelText), typeof(string), typeof(LoadingOverlay),
                new PropertyMetadata("ביטול"));

        public string CancelText
        {
            get => (string)GetValue(CancelTextProperty);
            set => SetValue(CancelTextProperty, value);
        }

        public static readonly DependencyProperty MinimumShowTimeMillisecondsProperty =
            DependencyProperty.Register(nameof(MinimumShowTimeMilliseconds), typeof(int), typeof(LoadingOverlay),
                new PropertyMetadata(250));

        /// <summary>Prevents quick show/hide flicker.</summary>
        public int MinimumShowTimeMilliseconds
        {
            get => (int)GetValue(MinimumShowTimeMillisecondsProperty);
            set => SetValue(MinimumShowTimeMillisecondsProperty, value);
        }

        // ---- Computed visibility helpers (no converters needed) ----
        public Visibility CancelVisibility => IsCancelable ? Visibility.Visible : Visibility.Collapsed;

        public Visibility HasDetailVisibility =>
            string.IsNullOrWhiteSpace(Detail) ? Visibility.Collapsed : Visibility.Visible;

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (LoadingOverlay)d;
            c.ApplyIsOpen((bool)e.NewValue);
        }

        private static void OnCancelableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LoadingOverlay)d).UpdateComputedVisibilities();
        }

        private static void OnDetailChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LoadingOverlay)d).UpdateComputedVisibilities();
        }

        private void UpdateComputedVisibilities()
        {
            CancelButton.Visibility = CancelVisibility;
            // Detail TextBlock visibility is bound to HasDetailVisibility property,
            // but we also want immediate UI update without needing converters/INPC.
            // Force binding refresh by re-setting DataContext to self.
            DataContext = null;
            DataContext = this;
        }

        private async void ApplyIsOpen(bool open)
        {
            UpdateComputedVisibilities();

            if (open)
            {
                _shownAtTicks = Stopwatch.GetTimestamp();
                OverlayRoot.Visibility = Visibility.Visible;

                if (Resources["ShowStoryboard"] is System.Windows.Media.Animation.Storyboard show)
                    show.Begin();

                await Task.Yield();
            }
            else
            {
                // if already collapsed, do nothing
                if (OverlayRoot.Visibility != Visibility.Visible)
                    return;

                if (Resources["HideStoryboard"] is System.Windows.Media.Animation.Storyboard hide)
                    hide.Begin();

                await Task.Delay(160);
                OverlayRoot.Visibility = Visibility.Collapsed;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            CancellationRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
