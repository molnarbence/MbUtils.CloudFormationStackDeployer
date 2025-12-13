using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Dunet;

namespace ConsoleApp.CloudFormation;

public static class AmazonCloudFormationClientExtensions
{
    extension(IAmazonCloudFormation client)
    {
        public async Task<StackExistsResult> StackExistsAsync(string stackName)
        {
            try
            {
                var response = await client.DescribeStacksAsync(new DescribeStacksRequest
                {
                    StackName = stackName
                });
                return response.Stacks.Count == 0
                    ? new StackExistsResult.No()
                    : new StackExistsResult.Yes();
            }
            catch (AmazonCloudFormationException ex) when (ex.ErrorCode == "ValidationError" &&
                                                           ex.Message.Contains("does not exist"))
            {
                return new StackExistsResult.No();
            }
            catch (Exception ex)
            {
                return new StackExistsResult.Error(ex.Message);
            }
        }

        public async Task<DeployResult> PollDeploymentStatusAsync(string stackName)
        {
            var stackStatus = StackStatus.CREATE_IN_PROGRESS;
        
            while (stackStatus.Value.EndsWith("_IN_PROGRESS"))
            {
                await Task.Delay(5000); // Wait for 5 seconds before polling again
            
                var response = await client.DescribeStacksAsync(new DescribeStacksRequest
                {
                    StackName = stackName
                });

                if (response.Stacks.Count == 0)
                {
                    return new DeployResult.Failure(new WorkflowErrors.AmazonError("Stack not found."));
                }

                stackStatus = response.Stacks[0].StackStatus;
            }
            return stackStatus.Value switch
            {
                "CREATE_COMPLETE" or "UPDATE_COMPLETE" => new DeployResult.Success(),
                "CREATE_FAILED" or "UPDATE_FAILED" or "ROLLBACK_COMPLETE" =>
                    new DeployResult.Failure(new WorkflowErrors.AmazonError($"Deployment failed with status: {stackStatus}")),
                _ => new DeployResult.Failure(new WorkflowErrors.AmazonError($"Unexpected stack status: {stackStatus}"))
            };
        }

        public async Task<StackDeletionResult> PollStackDeletionAsync(string stackName, CancellationToken cancellationToken)
        {
            var stackStatus = StackStatus.DELETE_IN_PROGRESS;
        
            while (stackStatus == StackStatus.DELETE_IN_PROGRESS && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000, cancellationToken); // Wait for 5 seconds before polling again
            
                try
                {
                    var response = await client.DescribeStacksAsync(new DescribeStacksRequest
                    {
                        StackName = stackName
                    }, cancellationToken);

                    if (response.Stacks.Count == 0)
                    {
                        return new StackDeletionResult.Success();
                    }

                    stackStatus = response.Stacks[0].StackStatus;
                }
                catch (AmazonCloudFormationException ex) when (ex.ErrorCode == "ValidationError" &&
                                                               ex.Message.Contains("does not exist"))
                {
                    return new StackDeletionResult.Success();
                }
                catch (Exception ex)
                {
                    return new StackDeletionResult.Failure(new WorkflowErrors.AmazonError(ex.Message));
                }
            }

            return stackStatus == StackStatus.DELETE_COMPLETE
                ? new StackDeletionResult.Success()
                : new StackDeletionResult.Failure(new WorkflowErrors.AmazonError($"Unexpected stack status: {stackStatus}"));
        }

        public async Task<string> GetStackOutputAsync(string stackName, string outputKey, CancellationToken cancellationToken)
        {
            var outputs = await client.GetStackOutputsAsync(stackName, cancellationToken);
            return outputs.TryGetValue(outputKey, out var outputValue) 
                ? outputValue 
                : throw new Exception($"Output key '{outputKey}' not found in stack '{stackName}'.");
        }

        public async Task<Dictionary<string, string>> GetStackOutputsAsync(string stackName, CancellationToken cancellationToken)
        {
            var response = await client.DescribeStacksAsync(new DescribeStacksRequest
            {
                StackName = stackName
            }, cancellationToken);

            if (response.Stacks.Count == 0)
            {
                throw new Exception($"Stack '{stackName}' not found.");
            }

            var stack = response.Stacks[0];
            return stack.Outputs?.ToDictionary(output => output.OutputKey, output => output.OutputValue) ?? new Dictionary<string, string>();
        }
    }
}

[Union]
public partial record StackExistsResult
{
    partial record Yes;
    partial record No;
    partial record Error(WorkflowErrors WorkflowError);
}