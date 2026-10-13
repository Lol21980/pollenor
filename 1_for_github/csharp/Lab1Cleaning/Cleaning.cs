using System.Globalization;
using System.Text.RegularExpressions;

namespace Lab1Cleaning;

public sealed class CleanRecord
{
    public string Country { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string? Region { get; set; }
    public string? IncomeGroup { get; set; }
    public string IndicatorCode { get; set; } = "";
    public string IndicatorName { get; set; } = "";
    public string? Topic { get; set; }
    public string? Definition { get; set; }
    public short? Year { get; set; }
    public float? Value { get; set; }
    public bool IsOutlier { get; set; }
}

public static class Cleaning
{
    static readonly Regex Ws = new(@"\s+", RegexOptions.Compiled);

    public static string? Norm(string? s)
    {
        if (s is null) return null;
        s = s.Trim();
        return s.Length == 0 ? null : s;
    }

    public static string? CleanText(string? s)
    {
        if (s is null) return null;
        s = Ws.Replace(s, " ").Trim();
        return s.Length == 0 ? null : s;
    }

    // "мар.73" -> "3.73", "ноя.27" -> "11.27"
    public static string FixExcelDate(string v)
    {
        v = v.Trim();
        int dot = v.IndexOf('.');
        if (dot > 0)
        {
            var head = v.Substring(0, dot).ToLowerInvariant();
            var key = head.Length >= 3 ? head.Substring(0, 3) : head;
            if (Config.Months.TryGetValue(key, out int m))
                return m + "." + v.Substring(dot + 1);
        }
        // также "мар 73" с пробелом
        var parts = v.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && Config.Months.TryGetValue(parts[0].ToLowerInvariant().Substring(0, Math.Min(3, parts[0].Length)), out int m2))
            return m2 + "." + parts[1];
        return v;
    }

    public static float? CoerceValue(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var v = raw.Trim();
        v = FixExcelDate(v);
        v = v.Replace(",", ".").Replace(" ", "");
        if (Config.HiddenNa.Contains(v)) return null;
        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
            return f;
        return null;
    }

    public static short? CoerceYear(string? raw)
    {
        if (short.TryParse((raw ?? "").Trim(), out short y)) return y;
        if (double.TryParse((raw ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
            return (short)d;
        return null;
    }

    public static (double q1, double q3) Quartiles(List<float> vals)
    {
        vals.Sort();
        double Q(double p)
        {
            double pos = (vals.Count - 1) * p;
            int lo = (int)Math.Floor(pos), hi = (int)Math.Ceiling(pos);
            if (lo == hi) return vals[lo];
            return vals[lo] + (vals[hi] - vals[lo]) * (pos - lo);
        }
        return (Q(0.25), Q(0.75));
    }
}
