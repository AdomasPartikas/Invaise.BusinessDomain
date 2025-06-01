using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for handling authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize("Admin")]
public class ServiceAccountController(IAuthService authService) : ControllerBase
{   
    /// <summary>
    /// Creates a new service account with the specified name and permissions.
    /// </summary>
    /// <param name="model">The data transfer object containing the name and permissions for the service account.</param>
    /// <returns>The created service account along with its key.</returns>
    [HttpPost]
    public async Task<ActionResult<ServiceAccountDto>> CreateServiceAccount([FromBody] CreateServiceAccountDto model)
    {
        var serviceAccount = await authService.ServiceRegisterAsync(model.Name, model.Permissions);
        
        // Return the account with the key (only time it's visible)
        return Ok(serviceAccount);
    }
} 