using System.Globalization;
using System.Text.RegularExpressions;

namespace ReportEditor.Models;

/// <summary>
/// SSRS size value like "4.2635cm". Preserves the original unit on round-trip.
/// </summary>
public readonly record struct SsrsUnit(double Value, string Unit)
{
    private static readonly Regex Re = new(@"^\s*(-?\d+(?:\.\d+)?)\s*(cm|mm|in|pt|pc|px)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ReNumberOpt = new(@"^\s*(-?\d+(?:\.\d+)?)\s*(cm|mm|in|pt|pc|px)?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Tries to parse a user-typed value like "12", "12mm", "0.5 in".</summary>
    public static bool TryParseSmart(string? s, string fallbackUnit, out SsrsUnit value)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            var m = ReNumberOpt.Match(s);
            if (m.Success)
            {
                var v = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                var u = string.IsNullOrEmpty(m.Groups[2].Value)
                    ? fallbackUnit
                    : m.Groups[2].Value.ToLowerInvariant();
                value = new SsrsUnit(v, u);
                return true;
            }
        }
        value = Zero(fallbackUnit);
        return false;
    }

    public static SsrsUnit Zero(string unit = "cm") => new(0, unit);

    public static SsrsUnit Parse(string? s, string defaultUnit = "cm")
    {
        if (string.IsNullOrWhiteSpace(s)) return Zero(defaultUnit);
        var m = Re.Match(s);
        if (!m.Success) return Zero(defaultUnit);
        return new SsrsUnit(
            double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
            m.Groups[2].Value.ToLowerInvariant());
    }

    /// <summary>Returns the value converted to millimetres.</summary>
    public double Mm => Unit switch
    {
        "mm" => Value,
        "cm" => Value * 10.0,
        "in" => Value * 25.4,
        "pt" => Value * 25.4 / 72.0,
        "pc" => Value * 25.4 / 6.0,
        "px" => Value * 25.4 / 96.0,
        _ => Value
    };

    /// <summary>Returns the value in CSS pixels (assuming 96 DPI).</summary>
    public double Px => Mm / 25.4 * 96.0;

    public static SsrsUnit FromMm(double mm, string unit = "cm")
    {
        var v = unit switch
        {
            "mm" => mm,
            "cm" => mm / 10.0,
            "in" => mm / 25.4,
            "pt" => mm / 25.4 * 72.0,
            "pc" => mm / 25.4 * 6.0,
            "px" => mm / 25.4 * 96.0,
            _ => mm
        };
        return new SsrsUnit(Math.Round(v, 5), unit);
    }

    public SsrsUnit WithMm(double mm) => FromMm(mm, Unit);

    public override string ToString()
        => Value.ToString("0.#####", CultureInfo.InvariantCulture) + Unit;
}
