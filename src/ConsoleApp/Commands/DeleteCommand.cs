using Amazon.SecurityToken;
using ConsoleApp.Configuration;
using ConsoleApp.SecurityToken;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ConsoleApp.Commands;

public class DeleteCommand(ConfigurationReader configurationReader, StackDeletionProcess stackDeletionProcess, IAmazonSecurityTokenService securityTokenService) : AsyncCommand<CommonCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CommonCommandSettings settings, CancellationToken cancellationToken)
    {
        var callerIdentity = await securityTokenService.GetCallerIdentityArnAsync(cancellationToken);
        AnsiConsole.MarkupLine($"Caller Identity Arn: [blue]{callerIdentity}[/]");
        
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