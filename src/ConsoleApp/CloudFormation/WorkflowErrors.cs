using Dunet;

namespace ConsoleApp.CloudFormation;


[Union]
public partial record WorkflowErrors
{
    partial record AmazonError(string Message);
}