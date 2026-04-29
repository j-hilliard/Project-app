namespace Stronghold.EnterpriseEstimating.Api.Middleware;

public static class CompanyOverrideMiddlewareExtensions
{
    public static IApplicationBuilder UseCompanyOverride(this IApplicationBuilder app) =>
        app.Use(async (ctx, next) =>
        {
            var overrideCode = ctx.Request.Headers["X-Company-Override"].FirstOrDefault();
            if (!string.IsNullOrEmpty(overrideCode) && ctx.User.Identity?.IsAuthenticated == true)
            {
                var claims = ctx.User.Claims
                    .Where(c => c.Type != "company_code")
                    .Append(new System.Security.Claims.Claim("company_code", overrideCode))
                    .ToList();
                var identity = new System.Security.Claims.ClaimsIdentity(
                    claims, ctx.User.Identity.AuthenticationType);
                ctx.User = new System.Security.Claims.ClaimsPrincipal(identity);
            }
            await next();
        });
}
