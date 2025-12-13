using Amazon.SecurityToken;

namespace ConsoleApp.SecurityToken;

public static class SecurityTokenExtensions
{
    extension(IAmazonSecurityTokenService service)
    {
        public async Task<string> GetCallerIdentityArnAsync(CancellationToken cancellationToken = default)
        {
            var response = await service.GetCallerIdentityAsync(new Amazon.SecurityToken.Model.GetCallerIdentityRequest(), cancellationToken);
            return response.Arn;
        }
    }
}