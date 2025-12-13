using System.Text.RegularExpressions;

namespace ConsoleApp;


public static partial class Patterns
{
    // match ${variables.*} or ${outputs.*}
    [GeneratedRegex(@"^\$\{(variables|outputs)\.(.*)}$")]
    public static partial Regex MacroPattern();
}