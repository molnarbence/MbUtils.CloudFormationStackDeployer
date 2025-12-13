using ConsoleApp.Configuration;

namespace ConsoleApp;

public static class Helpers
{
    public static string GetFullStackName(ProjectConfiguration projectConfiguration, string stackName) 
        => $"{projectConfiguration.StackPrefix}{stackName}";
    
    public static string ResolveVariable(ProjectConfiguration projectConfiguration, string rawValue)
    {
        var match = Patterns.VariablesPattern().Match(rawValue);
        if (!match.Success) return rawValue;
        var variableName = match.Groups[1].Value;
        return projectConfiguration.Variables.TryGetValue(variableName, out var variableValue)
            ? variableValue
            : throw new Exception($"Variable '{variableName}' not found for tag mapping.");
    }
}