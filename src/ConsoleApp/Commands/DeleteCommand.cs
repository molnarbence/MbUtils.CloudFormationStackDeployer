using ConsoleApp.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ConsoleApp.Commands;

public class DeleteCommand(ConfigurationReader configurationReader, StackDeletionProcess stackDeletionProcess) : AsyncCommand<DeleteCommand.Settings>
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
            .Title("Select a stack to delete:")
            .PageSize(10)
            .AddChoices(stackKeys);
        var selectedStackName = AnsiConsole.Prompt(stackSelection);
        
        AnsiConsole.MarkupLine($"Selected: [green]{selectedStackName}[/]");
        
        await stackDeletionProcess.ExecuteAsync(projectConfiguration, selectedStackName, cancellationToken);

        return 0;
    }
}