// ЛР1 на C#: повторяет Python-конвейер (см. src/pipeline.py, src/cleaning.py, src/db.py)
using Lab1Cleaning;
using System.Diagnostics;

bool sample = args.Contains("--sample");
Directory.CreateDirectory(Config.DataInterim);
Directory.CreateDirectory(Config.DataProcessed);

if (sample)
{
    var outCsv = Path.Combine(Config.DataInterim, "sample_200k_cs.csv");
    if (!File.Exists(outCsv))
    {
        int n = Pipeline.MakeSample(Config.RawFiles.Where(File.Exists).ToArray(), outCsv);
        Console.WriteLine($"срез готов, строк: {n}");
    }
    var recs = Pipeline.ReadAndClean(new[] { outCsv });
    var outClean = Path.Combine(Config.DataProcessed, "clean_sample_cs.csv");
    Pipeline.WriteCleanCsv(recs, outClean);
    Console.WriteLine($"готово {outClean}, строк: {recs.Count}");
}
else
{
    var sw = Stopwatch.StartNew();
    var recs = Pipeline.ReadAndClean(Config.RawFiles.Where(File.Exists).ToArray());
    var outClean = Path.Combine(Config.DataProcessed, "clean_cs.csv");
    Pipeline.WriteCleanCsv(recs, outClean);
    Console.WriteLine($"clean сохранён {outClean}, строк: {recs.Count}, {sw.Elapsed.TotalSeconds:F1}с");

    string db = Path.Combine(Config.DataProcessed, "dataset_cs.db");
    double t = Pipeline.LoadSqlite(recs, db);
    Console.WriteLine($"SQLite {db} за {t:F1}с, размер {new FileInfo(db).Length / 1024 / 1024} МБ");

    // контрольные запросы
    using var con = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={db}");
    con.Open();
    foreach (var q in new[] { "SELECT COUNT(*) FROM records", "SELECT COUNT(*) FROM countries", "SELECT COUNT(*) FROM indicators" })
    {
        using var cmd = con.CreateCommand(); cmd.CommandText = q;
        Console.WriteLine($"{q} -> {cmd.ExecuteScalar()}");
    }
    var sw2 = Stopwatch.StartNew();
    using (var cmd = con.CreateCommand()) { cmd.CommandText = "SELECT indicator_code, COUNT(*), AVG(value) FROM records GROUP BY indicator_code"; using var r = cmd.ExecuteReader(); int g = 0; while (r.Read()) g++; Console.WriteLine($"групп: {g}"); }
    Console.WriteLine($"агрегация {sw2.Elapsed.TotalSeconds:F2}с");
}
