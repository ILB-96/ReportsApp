
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Reports.Utilities
{
    /// <summary>
    /// Options that control how values are collected and formatted.
    /// </summary>
    public sealed class FieldCollectorOptions
    {
        /// <summary>Choose the key for each control: by Tag (string) or Name.</summary>
        public Func<FrameworkElement, string?> KeySelector { get; set; } =
            fe => (fe.Tag as string) ?? fe.Name;

        /// <summary>Filter which controls should be included.</summary>
        public Func<FrameworkElement, bool> IncludePredicate { get; set; } =
            fe => !string.IsNullOrWhiteSpace((fe.Tag as string) ?? fe.Name);

        /// <summary>Formatting culture (dates/numbers).</summary>
        public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

        /// <summary>Format booleans (default "true"/"false").</summary>
        public Func<bool, string> BooleanFormatter { get; set; } =
            b => b ? "true" : "false";

        /// <summary>Format dates (default dd/MM/yyyy).</summary>
        public Func<DateTime, string> DateFormatter { get; set; } =
            dt => dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        /// <summary>Per-key custom formatters (e.g., "ToggleOption" => Yes/No).</summary>
        public Dictionary<string, Func<string, string>> CustomFormatters { get; } =
            new(StringComparer.Ordinal);

        /// <summary>Trim text values (default true).</summary>
        public bool TrimText { get; set; } = true;
    }

    /// <summary>
    /// Collects key/value pairs from a visual tree and extracts values from common/third-party controls.
    /// </summary>
    public static class FormFieldCollector
    {
        // --- Reflection accessors (cached per control type) ---
        private static readonly ConcurrentDictionary<Type, Accessors> AccessorCache = new();

        private sealed class Accessors
        {
            public PropertyInfo? Text { get; init; }
            public PropertyInfo? IsChecked { get; init; }
            public PropertyInfo? SelectedDate { get; init; }
            public PropertyInfo? SelectedValue { get; init; }
            public PropertyInfo? SelectedItem { get; init; }
            public PropertyInfo? Content { get; init; }

            public static Accessors Create(Type t)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                return new Accessors
                {
                    Text = t.GetProperty("Text", flags),
                    IsChecked = t.GetProperty("IsChecked", flags),      // e.g., ToggleSwitch
                    SelectedDate = t.GetProperty("SelectedDate", flags),// e.g., DatePicker
                    SelectedValue = t.GetProperty("SelectedValue", flags),
                    SelectedItem = t.GetProperty("SelectedItem", flags),
                    Content = t.GetProperty("Content", flags),
                };
            }
        }

        /// <summary>
        /// Walks the visual tree under <paramref name="root"/> and returns a dictionary of {key -> value}.
        /// Key is taken from Tag (if string) else from Name. Value is extracted via <see cref="GetControlValue"/>.
        /// </summary>
        public static Dictionary<string, string> CollectFields(DependencyObject root, FieldCollectorOptions? options = null)
        {
            options ??= new FieldCollectorOptions();
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var fe in FindVisualChildren<FrameworkElement>(root))
            {
                if (!options.IncludePredicate(fe))
                    continue;

                var key = options.KeySelector(fe);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                var raw = GetControlValue(fe, options);
                var value = raw ?? string.Empty;

                if (options.CustomFormatters.TryGetValue(key, out var formatter))
                    value = formatter(value);

                result[key] = value;
            }

            return result;
        }

        /// <summary>
        /// Extracts a string value from common WPF controls and third-party controls via reflection.
        /// </summary>
        
        private static string GetControlValue(FrameworkElement fe, FieldCollectorOptions options)
        {
            // Fast paths for native WPF controls
            switch (fe)
            {
                case TextBox tb:
                {
                    var text = tb.Text ?? string.Empty;
                    return options.TrimText ? text.Trim() : text;
                }

                case PasswordBox pb:
                {
                    var text = pb.Password ?? string.Empty;
                    return options.TrimText ? text.Trim() : text;
                }

                case CheckBox cb:
                {
                    return options.BooleanFormatter(cb.IsChecked == true);
                }

                case DatePicker dp:
                {
                    if (dp.SelectedDate is DateTime dt)
                        return options.DateFormatter(dt);
                    return string.Empty;
                }

                case ComboBox combo:
                {
                    var s = combo.Text ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(s))
                        s = combo.SelectedValue?.ToString() ?? combo.SelectedItem?.ToString() ?? string.Empty;
                    return options.TrimText ? s.Trim() : s;
                }
            }

            // Reflection fallback for 3rd-party controls (e.g., WPF UI ToggleSwitch/TextBox)
            var acc = AccessorCache.GetOrAdd(fe.GetType(), Accessors.Create);

            // Text
            if (acc.Text != null)
            {
                var val = acc.Text.GetValue(fe) as string ?? string.Empty;
                return options.TrimText ? val.Trim() : val;
            }

            // IsChecked (bool / bool?)  <-- FIXED: no pattern variable for nullable types
            if (acc.IsChecked != null)
            {
                var raw = acc.IsChecked.GetValue(fe);

                if (raw is bool b1)
                    return options.BooleanFormatter(b1);

                if (raw is bool?)
                {
                    var nb = (bool?)raw;
                    return options.BooleanFormatter(nb == true);
                }
            }

            // SelectedDate (DateTime / DateTime?)  <-- also uses cast for nullable
            if (acc.SelectedDate != null)
            {
                var raw = acc.SelectedDate.GetValue(fe);

                if (raw is DateTime dt)
                    return options.DateFormatter(dt);

                if (raw is DateTime?)
                {
                    var ndt = (DateTime?)raw;
                    if (ndt.HasValue)
                        return options.DateFormatter(ndt.Value);
                }
            }

            // SelectedValue
            if (acc.SelectedValue != null)
            {
                var sv = acc.SelectedValue.GetValue(fe)?.ToString() ?? string.Empty;
                return options.TrimText ? sv.Trim() : sv;
            }

            // SelectedItem
            if (acc.SelectedItem != null)
            {
                var si = acc.SelectedItem.GetValue(fe)?.ToString() ?? string.Empty;
                return options.TrimText ? si.Trim() : si;
            }

            // Content (Buttons, Labels, etc.)
            if (acc.Content != null)
            {
                var c = acc.Content.GetValue(fe)?.ToString() ?? string.Empty;
                return options.TrimText ? c.Trim() : c;
            }

            return string.Empty;
        }


        /// <summary>
        /// Enumerates all visual descendants of type <typeparamref name="T"/> under <paramref name="root"/>.
        /// </summary>
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t)
                    yield return t;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        /// <summary>
        /// Convenience selector: Tag (string) or Name.
        /// </summary>
        public static string? KeyFromTagOrName(FrameworkElement fe) =>
            (fe.Tag as string) ?? fe.Name;
    }
}
