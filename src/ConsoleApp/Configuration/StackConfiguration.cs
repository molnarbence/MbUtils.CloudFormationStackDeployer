namespace ConsoleApp.Configuration;

public record StackConfiguration
{
    public required string Name { get; set; }
    public required string Template { get; set; }
    public Dictionary<string, string> Parameters { get; init; } = new();
}