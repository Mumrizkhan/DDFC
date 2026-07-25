using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace DDFC.API.Debugging
{
    public class AuthorizationDebugMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthorizationDebugMiddleware> _logger;

        public AuthorizationDebugMiddleware(RequestDelegate next, ILogger<AuthorizationDebugMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Log Authorization header (if any)
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            _logger.LogInformation("[AuthDebug] Request {Method} {Path} - Authorization: {AuthHeader}",
                context.Request.Method, context.Request.Path, string.IsNullOrEmpty(authHeader) ? "(none)" : authHeader);

            // Log authentication info
            var user = context.User;
            _logger.LogInformation("[AuthDebug] IsAuthenticated={IsAuthenticated}, Name={Name}",
                user?.Identity?.IsAuthenticated ?? false,
                user?.Identity?.Name ?? "(null)");

            // Log claims
            foreach (var claim in user?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
            {
                _logger.LogInformation("[AuthDebug] Claim: {Type} = {Value}", claim.Type, claim.Value);
            }

            await _next(context);
        }
    }
}
