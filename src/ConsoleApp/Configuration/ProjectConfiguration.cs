namespace ConsoleApp.Configuration;

public record ProjectConfiguration
{
    public required string StackPrefix { get; init; }
    public Dictionary<string, string> Variables { get; init; } = new();
    public Dictionary<string, string> Tags { get; init; } = new();
    public List<StackConfiguration> Stacks { get; init; } = [];
}