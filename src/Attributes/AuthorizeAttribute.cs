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
    private readonly string[] _requiredPermissions;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class.
    /// </summary>
    /// <param name="roles">Optional roles that are authorized to access the resource.</param>
    public AuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
        _requiredPermissions = [];
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class with specific permissions.
    /// </summary>
    /// <param name="roles">Optional roles that are authorized to access the resource.</param>
    /// <param name="permissions">Required permissions to access the resource.</param>
    public AuthorizeAttribute(string[] roles, string[] permissions)
    {
        _roles = roles;
        _requiredPermissions = permissions;
    }
    
    /// <summary>
    /// Creates an authorization attribute that requires specific permissions.
    /// </summary>
    /// <param name="permissions">The required permissions to access the resource.</param>
    /// <returns>An <see cref="AuthorizeAttribute"/> configured with the specified permissions.</returns>
    public static AuthorizeAttribute WithPermissions(params string[] permissions)
    {
        return new AuthorizeAttribute([], permissions);
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

        var user = (User?)context.HttpContext.Items["User"];
        var serviceAccount = (ServiceAccount?)context.HttpContext.Items["ServiceAccount"];
            
        // Get user from context
        if (user == null && serviceAccount == null)
        {
            // Not logged in
            context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }
        
        // Check role if specified
        if (_roles.Length > 0)
        {
            bool authorized = false;
            
            if (user != null && _roles.Contains(user.Role))
            {
                authorized = true;
            }
            
            if (serviceAccount != null && _roles.Contains(serviceAccount.Role))
            {
                authorized = true;
            }
            
            if (!authorized)
            {
                // Role not authorized
                context.Result = new JsonResult(new { message = "Forbidden" }) { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }
        }

        // Check permissions if specified
        if (_requiredPermissions.Length > 0 && serviceAccount != null)
        {
            if (!_requiredPermissions.All(p => serviceAccount.Permissions.Contains(p)))
            {
                // Service account doesn't have required permissions
                context.Result = new JsonResult(new { message = "Forbidden - Missing required permissions" }) 
                { 
                    StatusCode = StatusCodes.Status403Forbidden 
                };
                return;
            }
        }
        
        // Authorization successful
    }
} 