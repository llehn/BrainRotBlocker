using System.Globalization;
using System.Text;

namespace BrainRotBlocker.Core.Configuration;

/// <summary>
/// Parses human-friendly duration strings used in configuration, such as "2m",
/// "1h", "90s", or "1h30m". A plain <see cref="TimeSpan"/> string ("00:02:00")
/// is also accepted as a fallback.
/// </summary>
public static class Duration
{
    public static TimeSpan Parse(string text)
    {
        if (TryParse(text, out TimeSpan value))
        {
            return value;
        }

        throw new ConfigurationException(
            $"'{text}' is not a valid duration. Use forms like '2m', '1h', '90s', or '1h30m'.");
    }

    public static bool TryParse(string? text, out TimeSpan value)
    {
        value = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();

        // Fallback: a plain TimeSpan such as "00:02:00".
        if (text.Contains(':')
            && TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out TimeSpan ts))
        {
            value = ts;
            return true;
        }

        var total = TimeSpan.Zero;
        var number = new StringBuilder();
        bool sawComponent = false;

        foreach (char c in text)
        {
            if (char.IsDigit(c) || c == '.')
            {
                number.Append(c);
                continue;
            }

            if (number.Length == 0
                || !double.TryParse(
                    number.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double n))
            {
                return false;
            }

            TimeSpan? unit = char.ToLowerInvariant(c) switch
            {
                's' => TimeSpan.FromSeconds(n),
                'm' => TimeSpan.FromMinutes(n),
                'h' => TimeSpan.FromHours(n),
                'd' => TimeSpan.FromDays(n),
                _ => null,
            };

            if (unit is null)
            {
                return false;
            }

            total += unit.Value;
            number.Clear();
            sawComponent = true;
        }

        // A trailing bare number with no unit is invalid.
        if (number.Length > 0 || !sawComponent)
        {
            return false;
        }

        value = total;
        return true;
    }
}
