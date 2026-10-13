using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Lab1Cleaning;

public static class Pipeline
{
    static CsvConfiguration Cfg() => new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Delimiter = ";",
        Encoding = new UTF8Encoding(true),
        BadDataFound = null,
        MissingFieldFound = null,
    };

    public static int MakeSample(string[] inputs, string output, int n = 200000)
    {
        int written = 0;
        bool first = true;
        using var outWriter = new StreamWriter(output, false, new UTF8Encoding(true));
        foreach (var fp in inputs)
        {
            if (!File.Exists(fp) || written >= n) break;
            using var reader = new StreamReader(fp, Encoding.UTF8);
            using var csv = new CsvReader(reader, Cfg());
            csv.Read(); csv.ReadHeader();
            if (first)
            {
                outWriter.WriteLine(string.Join(";", csv.HeaderRecord!));
                first = false;
            }
            while (csv.Read() && written < n)
            {
                var row = new List<string>();
                for (int i = 0; i < csv.HeaderRecord!.Length; i++)
                    row.Add(csv.GetField(i) ?? "");
                outWriter.WriteLine(string.Join(";", row.Select(Escape)));
                written++;
            }
        }
        return written;
    }

    static string Escape(string s) => s.Contains(';') || s.Contains('"') || s.Contains('\n')
        ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    public static List<CleanRecord> ReadAndClean(string[] inputs, bool sampleOnly = false)
    {
        var records = new List<CleanRecord>(3_200_000);
        var seenFull = new HashSet<string>();
        var seenKey = new HashSet<string>();
        int inRows = 0, dupFull = 0, dupKey = 0;
        var sw = Stopwatch.StartNew();
        foreach (var fp in inputs)
        {
            if (!File.Exists(fp)) continue;
            Console.WriteLine($"читаю {Path.GetFileName(fp)}");
            using var reader = new StreamReader(fp, Encoding.UTF8);
            using var csv = new CsvReader(reader, Cfg());
            csv.Read(); csv.ReadHeader();
            while (csv.Read())
            {
                inRows++;
                string country = (csv.GetField("Страна") ?? "").Trim();
                string ccode = (csv.GetField("Код страны") ?? "").Trim();
                string? region = Cleaning.Norm(csv.GetField("Регион"));
                string? income = Cleaning.Norm(csv.GetField("Группа дохода"));
                string icode = (csv.GetField("Код показателя") ?? "").Trim();
                string iname = (csv.GetField("Название показателя") ?? "").Trim();
                string? topic = Cleaning.Norm(csv.GetField("Тема показателя"));
                string? def = Cleaning.CleanText(csv.GetField("Определение показателя"));
                var year = Cleaning.CoerceYear(csv.GetField("Год"));
                var val = Cleaning.CoerceValue(csv.GetField("Значение"));
                // сноска отбрасывается (пуста на 92%)

                string fullKey = $"{country}|{ccode}|{region}|{income}|{icode}|{year}|{val}";
                if (!seenFull.Add(fullKey)) { dupFull++; continue; }
                string key = $"{ccode}|{icode}|{year}";
                if (!seenKey.Add(key)) { dupKey++; continue; } // keep first (=last after sort in python; здесь порядок файла)

                records.Add(new CleanRecord
                {
                    Country = country, CountryCode = ccode, Region = region,
                    IncomeGroup = income, IndicatorCode = icode, IndicatorName = iname,
                    Topic = topic, Definition = def, Year = year, Value = val
                });
            }
        }
        Console.WriteLine($"строк на входе: {inRows}, полных дубл.: {dupFull}, по ключу: {dupKey}, осталось: {records.Count}, {sw.Elapsed.TotalSeconds:F1}с");

        // выбросы IQR флагом
        var vals = records.Where(r => r.Value.HasValue).Select(r => r.Value!.Value).ToList();
        var (q1, q3) = Cleaning.Quartiles(vals);
        double iqr = q3 - q1, lo = q1 - 1.5 * iqr, hi = q3 + 1.5 * iqr;
        int flags = 0;
        foreach (var r in records)
        {
            bool f = r.Value.HasValue && (r.Value < lo || r.Value > hi);
            r.IsOutlier = f;
            if (f) flags++;
        }
        Console.WriteLine($"выбросов помечено: {flags}, границы: {lo:F2} - {hi:F2}");
        Console.WriteLine($"пустых value: {records.Count(r => !r.Value.HasValue)}");
        return records;
    }

    public static void WriteCleanCsv(List<CleanRecord> recs, string path)
    {
        using var w = new StreamWriter(path, false, new UTF8Encoding(true));
        w.WriteLine("country;country_code;region;income_group;indicator_code;indicator_name;topic;definition;year;value;is_outlier");
        foreach (var r in recs)
            w.WriteLine($"{r.Country};{r.CountryCode};{r.Region};{r.IncomeGroup};{r.IndicatorCode};{Escape(r.IndicatorName)};{r.Topic};{Escape(r.Definition ?? "")};{r.Year};{(r.Value.HasValue ? r.Value.Value.ToString(CultureInfo.InvariantCulture) : "")};{(r.IsOutlier ? 1 : 0)}");
    }

    public static double LoadSqlite(List<CleanRecord> recs, string dbPath)
    {
        if (File.Exists(dbPath)) File.Delete(dbPath);
        var sw = Stopwatch.StartNew();
        using var con = new SqliteConnection($"Data Source={dbPath}");
        con.Open();
        var ddl = """
        CREATE TABLE countries(country_code TEXT PRIMARY KEY, country TEXT NOT NULL, region TEXT, income_group TEXT);
        CREATE TABLE indicators(indicator_code TEXT PRIMARY KEY, indicator_name TEXT NOT NULL, topic TEXT, definition TEXT);
        CREATE TABLE records(country_code TEXT NOT NULL, indicator_code TEXT NOT NULL, year INTEGER NOT NULL, value REAL, is_outlier INTEGER, PRIMARY KEY(country_code, indicator_code, year));
        """;
        using (var cmd = con.CreateCommand()) { cmd.CommandText = ddl; cmd.ExecuteNonQuery(); }

        using var tx = con.BeginTransaction();
        var countries = recs.GroupBy(r => r.CountryCode).Select(g => g.First()).ToList();
        foreach (var c in countries)
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "INSERT INTO countries VALUES($cc,$c,$r,$i)";
            cmd.Parameters.AddWithValue("$cc", c.CountryCode); cmd.Parameters.AddWithValue("$c", c.Country);
            cmd.Parameters.AddWithValue("$r", (object?)c.Region ?? DBNull.Value); cmd.Parameters.AddWithValue("$i", (object?)c.IncomeGroup ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
        foreach (var g in recs.GroupBy(r => r.IndicatorCode).Select(g => g.First()))
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "INSERT INTO indicators VALUES($ic,$n,$t,$d)";
            cmd.Parameters.AddWithValue("$ic", g.IndicatorCode); cmd.Parameters.AddWithValue("$n", g.IndicatorName);
            cmd.Parameters.AddWithValue("$t", (object?)g.Topic ?? DBNull.Value); cmd.Parameters.AddWithValue("$d", (object?)g.Definition ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
        foreach (var r in recs)
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "INSERT INTO records VALUES($cc,$ic,$y,$v,$o)";
            cmd.Parameters.AddWithValue("$cc", r.CountryCode); cmd.Parameters.AddWithValue("$ic", r.IndicatorCode);
            cmd.Parameters.AddWithValue("$y", (object?)r.Year ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$v", (object?)r.Value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$o", r.IsOutlier ? 1 : 0);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
        using (var cmd = con.CreateCommand()) { cmd.CommandText = "CREATE INDEX idx_records_indicator ON records(indicator_code); CREATE INDEX idx_records_year ON records(year);"; cmd.ExecuteNonQuery(); }
        sw.Stop();
        return sw.Elapsed.TotalSeconds;
    }
}
