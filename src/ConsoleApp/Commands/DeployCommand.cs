using Amazon.SecurityToken;
using ConsoleApp.Configuration;
using ConsoleApp.SecurityToken;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ConsoleApp.Commands;

public class DeployCommand(ConfigurationReader configurationReader, DeploymentProcess deploymentProcess, IAmazonSecurityTokenService securityTokenService) : AsyncCommand<CommonCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CommonCommandSettings settings, CancellationToken cancellationToken)
    {
        // print the current caller ID
        var callerIdentity = await securityTokenService.GetCallerIdentityArnAsync(cancellationToken);
        AnsiConsole.MarkupLine($"Caller Identity Arn: [blue]{callerIdentity}[/]");
                    
        var projectPath = new ProjectPath(settings.ProjectFile);
        
        var projectConfiguration = configurationReader.ReadProject(projectPath);

        var stackKeys = projectConfiguration.Stacks.Select(x => x.Name);

        var stackSelection = new SelectionPrompt<string>()
            .Title("Select a stack to deploy:")
            .PageSize(10)
            .AddChoices(stackKeys);
        var selectedStackName = AnsiConsole.Prompt(stackSelection);
        
        AnsiConsole.MarkupLine($"Selected: [green]{selectedStackName}[/]");
        
        await deploymentProcess.DeployAsync(projectConfiguration, projectPath, selectedStackName, cancellationToken);

        return 0;
    }
}
