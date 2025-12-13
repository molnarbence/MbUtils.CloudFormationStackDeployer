using Amazon.CloudFormation;
using Amazon.SecurityToken;
using ConsoleApp;
using ConsoleApp.Commands;
using ConsoleApp.Configuration;
using MbUtils.Extensions.SpectreConsole;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var consoleHost = new SpectreConsoleHost();

consoleHost.Services
    .AddSingleton<IAmazonCloudFormation>(new AmazonCloudFormationClient())
    .AddSingleton<IAmazonSecurityTokenService>(new AmazonSecurityTokenServiceClient())
    .AddSingleton<ConfigurationReader>()
    .AddSingleton<DeploymentProcess>()
    .AddSingleton<StackDeletionProcess>();

consoleHost.Configure(configurator =>
{
    configurator.AddCommand<DeployCommand>("deploy");
    configurator.AddCommand<DeleteCommand>("delete");
});

return await consoleHost.RunAsync(args);