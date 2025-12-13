using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConsoleApp.Configuration;

public class ConfigurationReader
{
    public ProjectConfiguration ReadProject(ProjectPath projectPath)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer.Deserialize<ProjectConfiguration>(File.ReadAllText(projectPath.FullPath));
    }
}