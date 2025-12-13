using Spectre.Console.Cli;

namespace ConsoleApp.Commands;

public class CommonCommandSettings : CommandSettings
{
    [CommandOption("--project-file", isRequired: false)]
    public string ProjectFile { get; set; } = "project.yml";
}