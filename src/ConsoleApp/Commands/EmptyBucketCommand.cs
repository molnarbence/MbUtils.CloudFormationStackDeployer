using Amazon.S3;
using Amazon.SecurityToken;
using ConsoleApp.Configuration;
using ConsoleApp.S3;
using ConsoleApp.SecurityToken;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ConsoleApp.Commands;

public class EmptyBucketCommand(IAmazonSecurityTokenService securityTokenService, ConfigurationReader configurationReader, IAmazonS3 s3Client) : AsyncCommand<CommonCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CommonCommandSettings settings, CancellationToken cancellationToken)
    {
        var callerIdentity = await securityTokenService.GetCallerIdentityArnAsync(cancellationToken);
        AnsiConsole.MarkupLine($"Caller Identity Arn: [blue]{callerIdentity}[/]");
        
        var projectPath = new ProjectPath(settings.ProjectFile);
        var projectConfiguration = configurationReader.ReadProject(projectPath);
        
        var bucketNames = projectConfiguration.BucketsToEmpty.Select(bucketName => Helpers.ResolveVariable(projectConfiguration, bucketName)).ToList();
        
        var bucketNameSelection = new SelectionPrompt<string>()
            .Title("Select a bucket to purge:")
            .PageSize(10)
            .AddChoices(bucketNames);
        var selectedBucketName = AnsiConsole.Prompt(bucketNameSelection);
        
        AnsiConsole.MarkupLine($"Selected: [green]{selectedBucketName}[/]");
        
        AnsiConsole.MarkupLine($"[yellow]Purging all objects from bucket [bold]{selectedBucketName}[/]...[/]");
        
        var totalObjectsPurged = 0;
        
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync($"[green]Purging bucket... {selectedBucketName}[/]",
                async ctx =>
                {
                    await foreach (var keyList in s3Client.EmptyBucketAsync(selectedBucketName, cancellationToken))
                    {
                        totalObjectsPurged += keyList.Count;
                        ctx.Status($"Purged {totalObjectsPurged} objects from bucket {selectedBucketName}...");
                    }
                }
            );
        AnsiConsole.MarkupLine($"All done! Purged a total of [bold]{totalObjectsPurged}[/] objects from bucket [bold]{selectedBucketName}[/].");

        return 0;
    }
}