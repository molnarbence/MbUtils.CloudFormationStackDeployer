using Dunet;

namespace ConsoleApp.CloudFormation;

[Union]
public partial record StackDeletionResult
{
    partial record Success;
    partial record Failure(WorkflowErrors WorkflowError);

    partial record NoPath;
}