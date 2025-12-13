using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using ConsoleApp.CloudFormation;
using ConsoleApp.Configuration;
using Dunet;
using Spectre.Console;

namespace ConsoleApp;

public class DeploymentProcess(IAmazonCloudFormation cloudFormation)
{
    public async Task DeployAsync(ProjectConfiguration projectConfiguration, ProjectPath projectPath, string selectedStackName, CancellationToken cancellationToken)
    {
        var deploymentContext = new DeploymentContext(projectConfiguration, projectPath, selectedStackName,
            cloudFormation, cancellationToken);
        
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync($"[green]Deploying stack... {deploymentContext.FullStackName}[/]", 
                async ctx => await DeployWithStatusAsync(ctx, deploymentContext, cancellationToken)
            );
    }

    private async Task DeployWithStatusAsync(StatusContext ctx, DeploymentContext deploymentContext, CancellationToken cancellationToken)
    {   
        // check if the stack exists and initiate create or update accordingly
        AnsiConsole.MarkupLine($"Checking if stack '[blue]{deploymentContext.FullStackName}[/]' exists...");
        var deployInitiated = await cloudFormation.StackExistsAsync(deploymentContext.FullStackName).MatchAsync(
            async _ =>
            {
                ctx.Status("Updating stack...");
                AnsiConsole.MarkupLine($"[yellow]Stack '{deploymentContext.FullStackName}' already exists. Initiating update...[/]");
                return await InitiateUpdateAsync(deploymentContext);
            },
            async _ =>
            {
                ctx.Status("Creating stack...");
                AnsiConsole.MarkupLine($"[green]Stack '{deploymentContext.FullStackName}' does not exist. Initiating creation...[/]");
                return await InitiateCreateAsync(deploymentContext);
            }, 
            error => Task.FromResult<InitiateDeployResult>(new InitiateDeployResult.Error(error.WorkflowError)));

        var deployResult = await deployInitiated.MatchAsync(
            async createInitiated =>
            {
                AnsiConsole.MarkupLine($"[green]Stack creation initiated successfully. Stack ID: {createInitiated.StackName}[/]");
                ctx.Status("Polling deployment status...");
                return await cloudFormation.PollDeploymentStatusAsync(createInitiated.StackName);
            },
            async updateInitiated =>
            {
                AnsiConsole.MarkupLine($"[green]Stack update initiated successfully for: {updateInitiated.StackName}[/]");
                ctx.Status("Polling deployment status...");
                return await cloudFormation.PollDeploymentStatusAsync(updateInitiated.StackName);
            },
            noActionNeeded =>
            {
                AnsiConsole.MarkupLine($"[yellow]No updates needed for stack: {deploymentContext.FullStackName}[/]");
                return Task.FromResult<DeployResult>(new DeployResult.Success());
            },
            error => Task.FromResult<DeployResult>(new DeployResult.Failure(error.WorkflowError)));
        
        var z = await deployResult.MatchAsync<DeployResult>(
            success =>
            {
                AnsiConsole.MarkupLine("[green]Stack deployment succeeded![/]");
                return success;

            }, failure =>
            {
                AnsiConsole.MarkupLine($"[red]Stack deployment failed!, Error: {failure.WorkflowError}[/]");
                return failure;
            });

        await z.MatchSuccess(async _ =>
        {
            var outputs = await cloudFormation.GetStackOutputsAsync(deploymentContext.FullStackName, cancellationToken);
            foreach (var output in outputs)
            {
                AnsiConsole.MarkupLine($"[blue]Output: {output.Key} = {output.Value}[/]");
            }
        }, () => Task.CompletedTask);
    }
    
    private async Task<InitiateDeployResult> InitiateCreateAsync(DeploymentContext deploymentContext)
    {
        var parameters = await deploymentContext.MapParametersAsync().ToListAsync();
        var request = new CreateStackRequest
        {
            Capabilities = [Capability.CAPABILITY_IAM, Capability.CAPABILITY_NAMED_IAM],
            StackName = deploymentContext.FullStackName,
            TemplateBody = File.ReadAllText(deploymentContext.TemplateFilePath),
            Tags = deploymentContext.GetTags(),
            Parameters = parameters
        };
        
        foreach (var parameter in request.Parameters)
        {
            AnsiConsole.MarkupLine($"[blue]Parameter: {parameter.ParameterKey} = {parameter.ParameterValue}[/]");
        }
        
        var response = await cloudFormation.CreateStackAsync(request);
        
        return new InitiateDeployResult.CreateInitiated(response.StackId);
    }
    
    private async Task<InitiateDeployResult> InitiateUpdateAsync(DeploymentContext deploymentContext)
    {
        var parameters = await deploymentContext.MapParametersAsync().ToListAsync();
        var request = new UpdateStackRequest
        {
            Capabilities = [Capability.CAPABILITY_IAM, Capability.CAPABILITY_NAMED_IAM],
            StackName = deploymentContext.FullStackName,
            TemplateBody = File.ReadAllText(deploymentContext.TemplateFilePath),
            Tags = deploymentContext.GetTags(),
            Parameters = parameters
        };

        try
        {
            await cloudFormation.UpdateStackAsync(request);
            return new InitiateDeployResult.UpdateInitiated(deploymentContext.FullStackName);
        }
        catch (AmazonCloudFormationException e) when (e.ErrorCode == "ValidationError" && 
                                                      e.Message.Contains("No updates are to be performed"))
        {
            return new InitiateDeployResult.NoActionNeeded(deploymentContext.FullStackName);
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Error initiating stack update: {e.Message}[/]");
            return new InitiateDeployResult.Error(new WorkflowErrors.AmazonError(e.Message));
        }
    }
}

[Union]
public partial record InitiateDeployResult
{
    partial record CreateInitiated(string StackName);
    partial record UpdateInitiated(string StackName);
    partial record NoActionNeeded(string StackName);
    partial record Error(WorkflowErrors WorkflowError);
}