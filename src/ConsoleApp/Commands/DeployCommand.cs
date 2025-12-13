using ConsoleApp.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ConsoleApp.Commands;

public class DeployCommand(ConfigurationReader configurationReader, DeploymentProcess deploymentProcess) : AsyncCommand<DeployCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<project-file>")]
        public required string ProjectFile { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var projectPath = new ProjectPath(settings.ProjectFile);
        
        var projectConfiguration = configurationReader.ReadProject(projectPath);

        var stackKeys = projectConfiguration.Stacks.Select(x => x.Name);

        var stackSelection = new SelectionPrompt<string>()
            .Title("Select a stack to deploy:")
            .PageSize(10)
            .AddChoices(stackKeys);
        var selectedStackName = AnsiConsole.Prompt(stackSelection);
        
        AnsiConsole.MarkupLine($"Selected: [green]{selectedStackName}[/]");
        
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(settings.ProjectFile)) ?? ".";
        
        await deploymentProcess.DeployAsync(projectConfiguration, projectPath, selectedStackName, cancellationToken);

        return 0;
    }
}
