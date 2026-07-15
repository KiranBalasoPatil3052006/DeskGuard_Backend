using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DeskGuardBackend.Middleware
{
    /// <summary>
    /// Scopes multitenant requests by extracting the CompanyId claim from the authenticated user
    /// and making it available in HttpContext.Items for downstream controllers or services.
    /// </summary>
    public class CompanyScopeMiddleware
    {
        private readonly RequestDelegate _next;

        public CompanyScopeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var companyIdClaim = context.User.FindFirst("CompanyId")?.Value;
                if (!string.IsNullOrEmpty(companyIdClaim) && long.TryParse(companyIdClaim, out var companyId))
                {
                    context.Items["CompanyId"] = companyId;
                }
            }

            await _next(context);
        }
    }
}
