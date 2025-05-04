using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Attributes;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for handling authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogController(IDatabaseService dbService) : ControllerBase
{   
    /// <summary>
    /// Gets all latest logs
    /// </summary>
    /// <returns>The logs</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Log>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLatestLogs(int count)
    {
        var isServiceAccount = HttpContext.Items["ServiceAccount"] != null;
        var currentUser = HttpContext.Items["User"] as User;
        
        // If this is a service account, we can proceed without user validation
        // If this is a regular user account, ensure the user is valid
        if (!isServiceAccount && currentUser == null)
            return Unauthorized(new { message = "Unauthorized" });

        var logs = await dbService.GetLatestLogsAsync(count);
            
        // Only admins can access logs and service accounts
        if (!isServiceAccount && currentUser!.Role != "Admin")
            return Forbid();
            
        return Ok(logs);
    }
} 