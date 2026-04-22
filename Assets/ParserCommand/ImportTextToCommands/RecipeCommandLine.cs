using System.Collections.Generic;

public sealed class RecipeCommandLine
{
    public int lineNumber;
    public string rawText;
    public string commandName;
    public List<string> args = new();
}