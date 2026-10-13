namespace Lab1Cleaning;

public static class Config
{
    public static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    public static string DataRaw => Path.Combine(Root, "data", "raw");
    public static string DataInterim => Path.Combine(Root, "data", "interim");
    public static string DataProcessed => Path.Combine(Root, "data", "processed");

    public static string[] RawFiles => new[]
    {
        Path.Combine(DataRaw, "1_датасет_население_возраст_и_пол.csv"),
        Path.Combine(DataRaw, "2_датасет_рождаемость_смертность_урбанизация.csv"),
        Path.Combine(DataRaw, "3_датасет_миграция_фертильность_брачность.csv"),
    };

    public static readonly Dictionary<string, int> Months = new(StringComparer.OrdinalIgnoreCase)
    {
        ["янв"] = 1, ["фев"] = 2, ["мар"] = 3, ["апр"] = 4, ["май"] = 5, ["июн"] = 6,
        ["июл"] = 7, ["авг"] = 8, ["сен"] = 9, ["окт"] = 10, ["ноя"] = 11, ["дек"] = 12,
    };

    public static readonly HashSet<string> HiddenNa = new(StringComparer.OrdinalIgnoreCase)
    {
        "-", "—", "–", "нет данных", "н/д", "N/A", "NA", "NULL", "null", "None", ""
    };
}
