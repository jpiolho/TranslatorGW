namespace TranslatorGW;

public class CsvRecord
{
    public int? StringId { get; set; }
    public string Text { get; set; }
}

public class DatabaseEntry
{
    public int LanguageId { get; set; }
    public int StringId { get; set; }
    public string Term { get; set; }
}

public class Translation
{
    public string Text { get; set; } = "";
    public IntPtr Pointer { get; set; } = IntPtr.Zero;

    public Translation(string text) { Text = text; }
}
