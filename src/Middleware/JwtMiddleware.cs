using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Invaise.BusinessDomain.API.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Invaise.BusinessDomain.API.Middleware;

/// <summary>
/// Middleware for handling JWT token authentication.
/// </summary>
public class JwtMiddleware(RequestDelegate next, IConfiguration configuration)
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="dbService">The database service.</param>
    public async Task Invoke(HttpContext context, IDatabaseService dbService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
            await AttachUserToContext(context, dbService, token);

        await next(context);
    }

    private async Task AttachUserToContext(HttpContext context, IDatabaseService dbService, string token)
    {
        try
        {
            var jwtKey = configuration["JWT:Key"];
            var jwtIssuer = configuration["JWT:Issuer"];
            var jwtAudience = configuration["JWT:Audience"];
            
            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
                return;
                
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(jwtKey);
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            var accountType = jwtToken.Claims.FirstOrDefault(x => x.Type == "role")?.Value;
        
            if (accountType == "Service")
            {
                // This is a service account
                var serviceAccountId = jwtToken.Claims.First(x => x.Type == "nameid").Value;

                context.Items["ServiceAccount"] = await dbService.GetServiceAccountAsync(serviceAccountId);
            }
            else
            {
                // This is a user account (existing code)
                var userId = jwtToken.Claims.First(x => x.Type == "nameid").Value;
                context.Items["User"] = await dbService.GetUserByIdAsync(userId);
            }
        }
        catch
        {
            // Do nothing if jwt validation fails
            // User is not attached to context so the request won't have access to secure routes
        }
    }
} 