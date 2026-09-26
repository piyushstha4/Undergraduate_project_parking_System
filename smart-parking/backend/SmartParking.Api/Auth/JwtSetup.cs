using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SmartParking.Api.Models;

namespace SmartParking.Api.Auth;

/// <summary>
/// JWT bearer authentication - the C# equivalent of middleware/auth.js.
/// [Authorize] replaces authenticate(), [Authorize(Roles = "...")] replaces authorize(...roles).
/// </summary>
public static class JwtSetup
{
    public static string GetSecret(IConfiguration config)
    {
        var secret = config["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("Jwt:Secret in appsettings.json must be at least 32 characters long.");
        return secret;
    }

    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecret(config)));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep claim names exactly as "id", "role", "name", "email"
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = AppClaims.Name,
                    RoleClaimType = AppClaims.Role,
                };
                options.Events = new JwtBearerEvents
                {
                    // Same JSON error bodies as the Node API: { "error": "..." }
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        var hasToken = ctx.Request.Headers.Authorization.ToString().StartsWith("Bearer ");
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await ctx.Response.WriteAsJsonAsync(new ErrorResponse(hasToken
                            ? "Invalid or expired session. Please log in again."
                            : "Authentication required."));
                    },
                    OnForbidden = async ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await ctx.Response.WriteAsJsonAsync(
                            new ErrorResponse("You do not have permission to perform this action."));
                    },
                };
            });

        services.AddAuthorization();
        services.AddSingleton<TokenService>();
        return services;
    }
}
