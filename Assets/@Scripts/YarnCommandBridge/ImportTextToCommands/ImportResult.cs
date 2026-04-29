using System.Collections.Generic;

public sealed class ImportResult
{
    public int parsedLineCount;
    public int importedCommandCount;
    public readonly List<string> warnings = new();
    public readonly List<string> errors = new();
}