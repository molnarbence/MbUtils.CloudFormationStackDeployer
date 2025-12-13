using Dunet;

namespace ConsoleApp.CloudFormation;

[Union]
public partial record DeployResult
{
    partial record Success;
    partial record Failure(WorkflowErrors WorkflowError);
}