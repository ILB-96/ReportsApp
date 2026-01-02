
using System;
using System.Globalization;

namespace Reports.Utilities
{
    public static class ReservationTransforms
    {
        public static void Apply(ref System.Collections.Generic.Dictionary<string, string> fields, bool isGoto)
        {
            // Extract or compute fields you need
            var start = fields.TryGetValue("Start", out var startVal) ? startVal : "";
            var end   = fields.TryGetValue("End", out var endVal) ? endVal : "";
            var km    = fields.TryGetValue("Km", out var kmVal) ? kmVal : "";

            // Date derived from "End"
            var date = ExtractDate(end);
            fields["Date"] = date; // override or add

            // Cost derived from Start, End, Km, toggle
            var cost = CalculateCost(start, end, km, isGoto ? "goto" : "autotel")
                       .ToString(CultureInfo.InvariantCulture);
            fields["Cost"] = cost;
        }

        public static string ExtractDate(string end)
        {
            // end can be "dd/MM/yyyy HH:mm" or "HH:mm dd/MM/yyyy"
            var parts = end.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return string.Empty;

            // If the first part contains time (has ':'), then date is likely the second part.
            // Otherwise, use the first part.
            if (parts[0].Contains(':'))
                return parts.Length > 1 ? parts[1] : parts[0];
            else
                return parts[0];
        }

        public static double CalculateCost(string start, string end, string km, string toggle)
        {
            DateTime startDt = ParseFlexibleDate(start);
            DateTime endDt   = ParseFlexibleDate(end);

            TimeSpan diff = endDt - startDt;

            int days = diff.Days;
            // round down minute fraction into hours (integer math—adjust if you want decimals)
            int hours = diff.Hours + (diff.Minutes / 60);

            double kmNum = 0.0;
            double.TryParse(km, NumberStyles.Float, CultureInfo.InvariantCulture, out kmNum);

            double cost = 0.0;

            if (string.Equals(toggle, "goto", StringComparison.OrdinalIgnoreCase))
            {
                cost = days * 150;
                cost += 1.3 * kmNum;

                if (hours >= 8)
                    cost += 150;
                else
                    cost += hours * 15;
            }
            else
            {
                // autotel rules
                days *= 24;
                hours += days;

                if (hours >= 3)
                {
                    cost += 99;
                    cost += 1.2 * kmNum;
                    hours -= 3;
                    cost += 20 * hours;
                }
                else
                {
                    // minutes calculation path
                    cost += (hours * 60) * 1.5;
                }
            }

            return cost;
        }

        public static DateTime ParseFlexibleDate(string input)
        {
            string[] formats = new[]
            {
                "dd/MM/yyyy HH:mm",
                "HH:mm dd/MM/yyyy",
            };

            return DateTime.ParseExact(
                input,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None
            );
        }
    }
}
