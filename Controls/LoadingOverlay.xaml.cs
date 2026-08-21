using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Reports.Controls
{
    public partial class LoadingOverlay : UserControl
    {
        private const int HideAnimationMs = 140;

        private int _version;        // guards nested/overlapping scopes
        private int _visualVersion;  // guards the hide animation race
        private long _shownAtTicks;
        private CancellationTokenSource? _cts;

        public LoadingOverlay()
        {
            InitializeComponent();
            UpdateComputedVisibilities();
        }

        // ---- Public API ----

        /// <summary>Raised when the user clicks Cancel. The token is already cancelled at this point.</summary>
        public event EventHandler? CancellationRequested;

        /// <summary>
        /// Token for the currently active scope. <see cref="CancellationToken.None"/> when nothing is showing.
        /// </summary>
        public CancellationToken Token => _cts?.Token ?? CancellationToken.None;

        public YieldAwaitable ShowAsync(string message, string? detail = null, bool cancelable = false)
        {
            StartSession(message, detail, cancelable);
            return Task.Yield();
        }

        public async Task HideAsync()
        {
            IsOpen = false;
            EndSession();
            await Task.Yield();
        }

        /// <summary>
        /// using var scope = Loading.BeginScope("שולח נתונים…", cancelable: true);
        /// await service.SubmitAsync(model, scope.Token);
        /// </summary>
        public LoadingScope BeginScope(string message, string? detail = null, bool cancelable = false)
        {
            StartSession(message, detail, cancelable);
            var captured = ++_version;
            return new LoadingScope(this, captured, Token);
        }

        public sealed class LoadingScope : IDisposable
        {
            private readonly LoadingOverlay _owner;
            private readonly int _capturedVersion;
            private bool _disposed;

            internal LoadingScope(LoadingOverlay owner, int capturedVersion, CancellationToken token)
            {
                _owner = owner;
                _capturedVersion = capturedVersion;
                Token = token;
            }

            public CancellationToken Token { get; }

            /// <summary>Update the message while the scope is running (e.g. between steps).</summary>
            public void Report(string message, string? detail = null)
            {
                if (_disposed || _capturedVersion != _owner._version) return;
                _owner.Message = message;
                _owner.Detail = detail;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _ = _owner.HideIfLatestAsync(_capturedVersion);
            }
        }

        // ---- Session handling ----

        private void StartSession(string message, string? detail, bool cancelable)
        {
            // A previous scope may still be alive; replacing it is intentional.
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            Message = message;
            Detail = detail;
            IsCancelable = cancelable;

            CancelButton.IsEnabled = true;

            // Re-setting IsOpen to the same value doesn't raise the DP callback,
            // so refresh the timestamp explicitly for back-to-back scopes.
            _shownAtTicks = Stopwatch.GetTimestamp();

            if (IsOpen)
                UpdateComputedVisibilities();
            else
                IsOpen = true;
        }

        private void EndSession()
        {
            _cts?.Dispose();
            _cts = null;
        }

        private async Task HideIfLatestAsync(int capturedVersion)
        {
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
            EndSession();
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
                new PropertyMetadata(string.Empty, OnVisualStateChanged));

        public string? Detail
        {
            get => (string?)GetValue(DetailProperty);
            set => SetValue(DetailProperty, value);
        }

        public static readonly DependencyProperty OverlayBackgroundProperty =
            DependencyProperty.Register(nameof(OverlayBackground), typeof(Brush), typeof(LoadingOverlay),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00)),
                    OnVisualStateChanged));

        public Brush OverlayBackground
        {
            get => (Brush)GetValue(OverlayBackgroundProperty);
            set => SetValue(OverlayBackgroundProperty, value);
        }

        public static readonly DependencyProperty IsBlockingProperty =
            DependencyProperty.Register(nameof(IsBlocking), typeof(bool), typeof(LoadingOverlay),
                new PropertyMetadata(true, OnVisualStateChanged));

        /// <summary>When true, the backdrop swallows clicks aimed at the content underneath.</summary>
        public bool IsBlocking
        {
            get => (bool)GetValue(IsBlockingProperty);
            set => SetValue(IsBlockingProperty, value);
        }

        public static readonly DependencyProperty IsCancelableProperty =
            DependencyProperty.Register(nameof(IsCancelable), typeof(bool), typeof(LoadingOverlay),
                new PropertyMetadata(false, OnVisualStateChanged));

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

        public static readonly DependencyProperty CancelingMessageProperty =
            DependencyProperty.Register(nameof(CancelingMessage), typeof(string), typeof(LoadingOverlay),
                new PropertyMetadata("מבטל…"));

        /// <summary>Shown after Cancel is clicked, while the operation unwinds.</summary>
        public string CancelingMessage
        {
            get => (string)GetValue(CancelingMessageProperty);
            set => SetValue(CancelingMessageProperty, value);
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

        // ---- Visual state ----

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((LoadingOverlay)d).ApplyIsOpen((bool)e.NewValue);

        private static void OnVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((LoadingOverlay)d).UpdateComputedVisibilities();

        private void UpdateComputedVisibilities()
        {
            CancelButton.Visibility = IsCancelable ? Visibility.Visible : Visibility.Collapsed;

            DetailText.Visibility = string.IsNullOrWhiteSpace(Detail)
                ? Visibility.Collapsed
                : Visibility.Visible;

            // Null background => hit testing falls through the backdrop to the page,
            // while the Card (which has a brush) still receives the Cancel click.
            OverlayRoot.Background = IsBlocking ? OverlayBackground : null;
        }

        private async void ApplyIsOpen(bool open)
        {
            var version = ++_visualVersion;
            UpdateComputedVisibilities();

            if (open)
            {
                _shownAtTicks = Stopwatch.GetTimestamp();
                OverlayRoot.Visibility = Visibility.Visible;

                if (Resources["ShowStoryboard"] is Storyboard show)
                    show.Begin();
            }
            else
            {
                if (OverlayRoot.Visibility != Visibility.Visible)
                    return;

                if (Resources["HideStoryboard"] is Storyboard hide)
                    hide.Begin();

                await Task.Delay(HideAnimationMs);

                // Something re-opened us while the hide animation was running.
                if (version != _visualVersion)
                    return;

                OverlayRoot.Visibility = Visibility.Collapsed;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cts is null || _cts.IsCancellationRequested) return;

            _cts.Cancel();

            // Cancellation is cooperative — the work keeps going until it reaches the
            // next check, so stay visible and just reflect that we're winding down.
            CancelButton.IsEnabled = false;
            Message = CancelingMessage;
            Detail = null;

            CancellationRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}