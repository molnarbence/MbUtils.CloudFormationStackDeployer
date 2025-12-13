namespace ConsoleApp.Configuration;

public record ProjectConfiguration
{
    public required string StackPrefix { get; init; }
    public Dictionary<string, string> Variables { get; init; } = new();
    public Dictionary<string, string> StackTags { get; init; } = new();
    public List<StackConfiguration> Stacks { get; init; } = [];
    public List<string> BucketsToPurge { get; init; } = [];
}