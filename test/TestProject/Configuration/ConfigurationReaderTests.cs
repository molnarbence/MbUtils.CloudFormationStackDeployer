using ConsoleApp;
using ConsoleApp.Configuration;

namespace TestProject.Configuration;

public class ConfigurationReaderTests
{
    [Fact]
    public void Test_ReadProject()
    {
        // arrange
        var configReader = new ConfigurationReader();
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory)
            .Parent?.Parent?.Parent?.Parent?.Parent?.FullName ?? string.Empty;

        var projectPath = new ProjectPath(Path.Combine(dir, "sample-project-dir", "project.yml"));
        
        // act
        var project = configReader.ReadProject(projectPath);
        
        // assert
        Assert.Equal("sample-app-", project.StackPrefix);
        Assert.Equal("molnarbence-sample-storage", project.Variables["bucketName"]);
        Assert.Equal("SampleProject", project.Tags["Project"]);

        var firstStack = project.Stacks[0];
        Assert.Equal("iam-role", firstStack.Name);
        Assert.Equal("templates/iam-role.yml", firstStack.Template);
        Assert.Equal("${variables.bucketName}", firstStack.Parameters["BucketName"]);
    }
}