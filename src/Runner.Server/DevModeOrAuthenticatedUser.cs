using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

public class DevModeOrAuthenticatedUserRequirement : IAuthorizationRequirement
{
}

public class DevModeOrAuthenticatedUser : AuthorizationHandler<DevModeOrAuthenticatedUserRequirement>
{
    private bool useOpenIdConnect;
    public DevModeOrAuthenticatedUser(IConfiguration configuration)
    {
        useOpenIdConnect = configuration["Authority"] != null && configuration["ClientId"] != null  && configuration["ClientSecret"] != null;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DevModeOrAuthenticatedUserRequirement requirement)
    {
        if (!useOpenIdConnect || context.User.Identities.Any(x => x.IsAuthenticated))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}