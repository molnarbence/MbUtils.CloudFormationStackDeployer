using ConsoleApp;

namespace TestProject;

public class MacroPatternTests
{
    [Fact]
    public void Test_Matches_Variable()
    {
        // arrange
        const string parameter = "${variables.my-param}";
        
        // act
        var match = Patterns.AllPlaceholdersPattern().Match(parameter);
        
        // assert
        Assert.Equal("variables", match.Groups[1].Value);
        Assert.Equal("my-param", match.Groups[2].Value);
    }

    [Fact]
    public void Test_Matches_Outputs()
    {
        // arrange
        const string parameter = "${outputs.iam-role.RoleARN}";
        
        // act
        var match = Patterns.AllPlaceholdersPattern().Match(parameter);
        
        // assert
        Assert.Equal("outputs", match.Groups[1].Value);
        Assert.Equal("iam-role.RoleARN", match.Groups[2].Value);
    }
}