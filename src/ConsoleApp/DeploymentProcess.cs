using System.Runtime.CompilerServices;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using ConsoleApp.CloudFormation;
using ConsoleApp.Configuration;
using Dunet;
using Spectre.Console;

namespace ConsoleApp;

public class DeploymentProcess(IAmazonCloudFormation cloudFormation)
{
    private readonly Dictionary<string, Dictionary<string, string>> _stackOutputsCache = new();
    
    public async Task DeployAsync(ProjectConfiguration projectConfiguration, ProjectPath projectPath, string selectedStackName, CancellationToken cancellationToken)
    {
        var selectedStack = projectConfiguration.Stacks.First(stack => stack.Name == selectedStackName);

        var templateFilePath = projectPath.GetTemplateFilePath(selectedStack.Template);
        var fullStackName = Helpers.GetFullStackName(projectConfiguration, selectedStack.Name);
        
        var parameters = await MapParametersAsync(projectConfiguration, selectedStack, cancellationToken).ToListAsync(cancellationToken: cancellationToken);
        var tags = projectConfiguration
            .StackTags
            .Select(tag => new Tag { Key = tag.Key, Value = ResolveTagValue(projectConfiguration.Variables, tag.Value) }).ToList();
        
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync($"[green]Deploying stack... {fullStackName}[/]", async ctx =>
            {
                // check if the stack exists and initiate create or update accordingly
                AnsiConsole.MarkupLine($"Checking if stack '[blue]{fullStackName}[/]' exists...");
                var deployInitiated = await cloudFormation.StackExistsAsync(fullStackName).MatchAsync(
                    async _ =>
                    {
                        ctx.Status("Updating stack...");
                        AnsiConsole.MarkupLine($"[yellow]Stack '{fullStackName}' already exists. Initiating update...[/]");
                        return await InitiateUpdateAsync(templateFilePath, fullStackName, parameters, tags);
                    },
                    async _ =>
                    {
                        ctx.Status("Creating stack...");
                        AnsiConsole.MarkupLine($"[green]Stack '{fullStackName}' does not exist. Initiating creation...[/]");
                        return await InitiateCreateAsync(templateFilePath, fullStackName, parameters, tags);
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
                        AnsiConsole.MarkupLine($"[yellow]No updates needed for stack: {fullStackName}[/]");
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
                    var outputs = await cloudFormation.GetStackOutputsAsync(fullStackName, cancellationToken);
                    foreach (var output in outputs)
                    {
                        AnsiConsole.MarkupLine($"[blue]Output: {output.Key} = {output.Value}[/]");
                    }
                }, () => Task.CompletedTask);
            });
    }
    
    private async Task<string> ResolveParameterValueAsync(string value, ProjectConfiguration projectConfiguration, CancellationToken cancellationToken)
    {
        var match = Patterns.AllPlaceholdersPattern().Match(value);
        if (!match.Success) return value;
        
        var type = match.Groups[1].Value;
        var key = match.Groups[2].Value;

        switch (type)
        {
            case "variables":
                return projectConfiguration.Variables.TryGetValue(key, out var variableValue)
                    ? variableValue
                    : throw new Exception($"Variable '{key}' not found.");
            case "outputs":
            {
                // split key into stackName.outputKey
                var parts = key.Split('.', 2);
                if (parts.Length != 2)
                {
                    throw new Exception($"Invalid output macro format '{key}'. Expected format is 'stackName.outputKey'.");
                }
                var stackName = parts[0];
                var outputKey = parts[1];
                var fullStackName = Helpers.GetFullStackName(projectConfiguration, stackName);

                var stackOutputs = await GetStackOutputsAsync();

                return stackOutputs.TryGetValue(outputKey, out var output)
                    ? output
                    : throw new Exception($"Output '{outputKey}' of stack '${fullStackName}' not found.");

                async ValueTask<Dictionary<string, string>> GetStackOutputsAsync()
                {
                    if (_stackOutputsCache.TryGetValue(stackName, out var outputs))
                    {
                        return outputs;
                    }
                    var fromService = await cloudFormation.GetStackOutputsAsync(fullStackName, cancellationToken);
                    _stackOutputsCache[stackName] = fromService;
                    return fromService;
                }
            }
            default:
                throw new Exception($"Unknown macro type '{type}'.");
        }
    }
    
    private async Task<InitiateDeployResult> InitiateCreateAsync(string templateFilePath, string fullStackName, List<Parameter> parameters, List<Tag> tags)
    {
        var request = new CreateStackRequest
        {
            Capabilities = [Capability.CAPABILITY_IAM, Capability.CAPABILITY_NAMED_IAM],
            StackName = fullStackName,
            TemplateBody = File.ReadAllText(templateFilePath),
            Tags = tags,
            Parameters = parameters
        };
        
        foreach (var parameter in request.Parameters)
        {
            AnsiConsole.MarkupLine($"[blue]Parameter: {parameter.ParameterKey} = {parameter.ParameterValue}[/]");
        }
        
        var response = await cloudFormation.CreateStackAsync(request);
        
        return new InitiateDeployResult.CreateInitiated(response.StackId);
    }
    
    private async Task<InitiateDeployResult> InitiateUpdateAsync(string templateFilePath, string fullStackName, List<Parameter> parameters, List<Tag> tags)
    {
        var request = new UpdateStackRequest
        {
            Capabilities = [Capability.CAPABILITY_IAM, Capability.CAPABILITY_NAMED_IAM],
            StackName = fullStackName,
            TemplateBody = File.ReadAllText(templateFilePath),
            Tags = tags,
            Parameters = parameters
        };

        try
        {
            await cloudFormation.UpdateStackAsync(request);
            return new InitiateDeployResult.UpdateInitiated(fullStackName);
        }
        catch (AmazonCloudFormationException e) when (e.ErrorCode == "ValidationError" && 
                                                      e.Message.Contains("No updates are to be performed"))
        {
            return new InitiateDeployResult.NoActionNeeded(fullStackName);
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Error initiating stack update: {e.Message}[/]");
            return new InitiateDeployResult.Error(new WorkflowErrors.AmazonError(e.Message));
        }
    }

    private async IAsyncEnumerable<Parameter> MapParametersAsync(ProjectConfiguration projectConfiguration,
        StackConfiguration stackConfiguration, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var parameter in stackConfiguration.Parameters)
        {
            var resolvedValue = await ResolveParameterValueAsync(parameter.Value, projectConfiguration, cancellationToken);
            yield return new Parameter
            {
                ParameterKey = parameter.Key,
                ParameterValue = resolvedValue
            };
        }
    }

    private static string ResolveTagValue(Dictionary<string, string> variables, string tagValue)
    {
        var match = Patterns.VariablesPattern().Match(tagValue);
        if (!match.Success) return tagValue;
        var variableName = match.Groups[1].Value;
        return variables.TryGetValue(variableName, out var variableValue)
            ? variableValue
            : throw new Exception($"Variable '{variableName}' not found for tag mapping.");
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