using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using ConsoleApp.CloudFormation;
using ConsoleApp.Configuration;
using Dunet;
using Spectre.Console;

namespace ConsoleApp;

public class StackDeletionProcess(IAmazonCloudFormation cloudFormation)
{
    public async Task ExecuteAsync(ProjectConfiguration projectConfiguration, string selectedStackName, CancellationToken cancellationToken)
    {
        var selectedStack = projectConfiguration.Stacks.First(stack => stack.Name == selectedStackName);
        var fullStackName = Helpers.GetFullStackName(projectConfiguration, selectedStack.Name);
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Grenade)
            .StartAsync("Check if stack exists", async ctx =>
            {
                // check if the stack exists
                AnsiConsole.MarkupLine($"[grey]Checking if stack [bold]{fullStackName}[/] exists...[/]");

                var stackExistsResult = await cloudFormation.StackExistsAsync(fullStackName);

                stackExistsResult.Match(_ =>
                {
                    AnsiConsole.MarkupLine($"[green]Stack [bold]{fullStackName}[/] exists. Deleting stack...[/]");
                }, _ =>
                {
                    AnsiConsole.MarkupLine("[yellow]Stack does not exist. Nothing to delete.[/]");
                }, _ =>
                {
                    AnsiConsole.MarkupLine("[red]Error checking stack existence. Aborting deletion.[/]");
                });

                var deleteStackInitiationResult = await stackExistsResult.MatchYes<Task<DeleteStackInitiationResult>>(async _ =>
                {
                    ctx.Status("Deleting stack...");
                    await cloudFormation.DeleteStackAsync(new DeleteStackRequest
                    {
                        StackName = fullStackName,
                    }, cancellationToken);
                    return new DeleteStackInitiationResult.Initiated(fullStackName);
                }, () => Task.FromResult<DeleteStackInitiationResult>(new DeleteStackInitiationResult.NoPath()));

                deleteStackInitiationResult.Match(_ =>
                {
                    AnsiConsole.MarkupLine("[green]Stack deletion initiated successfully.[/]");
                }, _ =>
                {
                    AnsiConsole.MarkupLine("[red]Error while deleting stack.[/]");
                }, _ => {});

                var stackDeletionResult = await deleteStackInitiationResult.MatchInitiated<Task<CloudFormation.StackDeletionResult>>(async _ =>
                {
                    ctx.Status("Polling stack deletion status...");
                    return await cloudFormation.PollStackDeletionAsync(fullStackName, cancellationToken);
                }, () => Task.FromResult<CloudFormation.StackDeletionResult>(new CloudFormation.StackDeletionResult.NoPath()));

                stackDeletionResult.Match(_ =>
                {
                    AnsiConsole.MarkupLine("[green]Stack deletion completed successfully.[/]");
                }, _ => AnsiConsole.MarkupLine("[red]Stack deletion failed.[/]"), _ => { });
                
            });
    }
}


[Union]
public partial record DeleteStackInitiationResult
{
    partial record Initiated(string StackName);
    partial record Failure(WorkflowErrors WorkflowError);

    partial record NoPath;
}