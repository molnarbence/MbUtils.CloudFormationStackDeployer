using ConsoleApp.Configuration;

namespace ConsoleApp;

public static class Helpers
{
    public static string GetFullStackName(ProjectConfiguration projectConfiguration, string stackName) 
        => $"{projectConfiguration.StackPrefix}{stackName}";
}