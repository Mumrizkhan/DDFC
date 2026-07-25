using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DDFC.API.Debugging
{
    public class DebugAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
        private readonly ILogger<DebugAuthorizationMiddlewareResultHandler> _logger;

        public DebugAuthorizationMiddlewareResultHandler(ILogger<DebugAuthorizationMiddlewareResultHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            // Log user info, claims, and policy
            _logger.LogInformation("Authorization Debug: Path={Path}, User={User}, Authenticated={Auth}, Policy={Policy}, Succeeded={Succeeded}",
                context.Request.Path,
                context.User.Identity?.Name,
                context.User.Identity?.IsAuthenticated,
                policy.Requirements,
                authorizeResult.Succeeded);

            foreach (var claim in context.User.Claims)
            {
                _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
            }

            // You can set a breakpoint here or add more detailed logging as needed

            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}
