using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using ConsoleApp.CloudFormation;
using ConsoleApp.Configuration;

namespace ConsoleApp;

public class DeploymentContext
{
    private readonly ProjectConfiguration _projectConfiguration;
    private readonly ProjectPath _projectPath;
    private readonly CancellationToken _cancellationToken;
    private readonly StackConfiguration _selectedStackConfiguration;
    private readonly IAmazonCloudFormation _cloudFormation;
    private readonly Dictionary<string, Dictionary<string, string>> _stackOutputsCache = new();

    public DeploymentContext(ProjectConfiguration projectConfiguration, ProjectPath projectPath, string selectedStackName, IAmazonCloudFormation cloudFormation, CancellationToken cancellationToken = default)
    {
        _projectConfiguration = projectConfiguration;
        _projectPath = projectPath;
        _cloudFormation = cloudFormation;
        _selectedStackConfiguration = projectConfiguration.Stacks.First(stack => stack.Name == selectedStackName);
        _cancellationToken = cancellationToken;
    }

    public string FullStackName => $"{_projectConfiguration.StackPrefix}{_selectedStackConfiguration.Name}";
    
    public async IAsyncEnumerable<Parameter> MapParametersAsync()
    {
        foreach (var parameter in _selectedStackConfiguration.Parameters)
        {
            var resolvedValue = await ResolveParameterValueAsync(parameter.Value);
            yield return new Parameter
            {
                ParameterKey = parameter.Key,
                ParameterValue = resolvedValue
            };
        }
    }

    public List<Tag> GetTags()
    {
        return _projectConfiguration
            .StackTags
            .Select(tag => new Tag { Key = tag.Key, Value = ResolveTagValue(tag.Value) }).ToList();
    }

    public string TemplateFilePath => _projectPath.GetTemplateFilePath(_selectedStackConfiguration.Template);

    private async Task<string> ResolveParameterValueAsync(string value)
    {
        var match = Patterns.AllPlaceholdersPattern().Match(value);
        if (!match.Success) return value;
        
        var type = match.Groups[1].Value;
        var key = match.Groups[2].Value;

        switch (type)
        {
            case "variables":
                return _projectConfiguration.Variables.TryGetValue(key, out var variableValue)
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

                var stackOutputs = await GetStackOutputsAsync();

                return stackOutputs.TryGetValue(outputKey, out var output)
                    ? output
                    : throw new Exception($"Output '{outputKey}' of stack '${FullStackName}' not found.");

                async ValueTask<Dictionary<string, string>> GetStackOutputsAsync()
                {
                    if (_stackOutputsCache.TryGetValue(stackName, out var outputs))
                    {
                        return outputs;
                    }
                    var fromService = await _cloudFormation.GetStackOutputsAsync(FullStackName, _cancellationToken);
                    _stackOutputsCache[stackName] = fromService;
                    return fromService;
                }
            }
            default:
                throw new Exception($"Unknown macro type '{type}'.");
        }
    }
    
    
    private string ResolveTagValue(string tagValue)
    {
        var match = Patterns.VariablesPattern().Match(tagValue);
        if (!match.Success) return tagValue;
        var variableName = match.Groups[1].Value;
        return _projectConfiguration.Variables.TryGetValue(variableName, out var variableValue)
            ? variableValue
            : throw new Exception($"Variable '{variableName}' not found for tag mapping.");
    }
}