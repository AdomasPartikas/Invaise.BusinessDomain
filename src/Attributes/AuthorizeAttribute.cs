using Invaise.BusinessDomain.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;

namespace Invaise.BusinessDomain.API.Attributes;

/// <summary>
/// Authorization attribute that validates the user is authenticated.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class.
    /// </summary>
    /// <param name="roles">Optional roles that are authorized to access the resource.</param>
    public AuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }
    
    /// <summary>
    /// Called early in the filter pipeline to confirm request is authorized.
    /// </summary>
    /// <param name="context">The authorization filter context.</param>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Skip authorization if action is decorated with [AllowAnonymous] attribute
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType() == typeof(AllowAnonymousAttribute));
        if (allowAnonymous)
            return;
            
        // Get user from context
        var user = (User?)context.HttpContext.Items["User"];
        if (user == null)
        {
            // Not logged in
            context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }
        
        // Check role if specified
        if (_roles.Any() && !_roles.Contains(user.Role))
        {
            // Role not authorized
            context.Result = new JsonResult(new { message = "Forbidden" }) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
} 