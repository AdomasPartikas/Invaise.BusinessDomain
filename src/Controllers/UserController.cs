using Invaise.BusinessDomain.API.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Attributes;
using AutoMapper;

namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for handling user related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController(IDatabaseService dbService, IMapper mapper) : ControllerBase
{   
    /// <summary>
    /// Gets the current user's information.
    /// </summary>
    /// <returns>The user information.</returns>
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var user = (User)HttpContext.Items["User"]!;
        return Ok(mapper.Map<UserDto>(user));
    }
    
    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The user information.</returns>
    [HttpGet("{id}")]
    [Authorize("Admin")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var user = await dbService.GetUserByIdAsync(id, includeInactive: true);
        if (user == null)
            return NotFound(new { message = "User not found" });
            
        return Ok(mapper.Map<UserDto>(user));
    }

    /// <summary>
    /// Gets all users (admin only).
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive users.</param>
    /// <returns>All users.</returns>
    [HttpGet]
    [Authorize("Admin")]
    public async Task<IActionResult> GetAllUsers([FromQuery] bool includeInactive = false)
    {
        var users = await dbService.GetAllUsersAsync(includeInactive);
        return Ok(users.Select(mapper.Map<UserDto>));
    }
    
    /// <summary>
    /// Updates a user's active status (admin only).
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="isActive">The new active status.</param>
    /// <returns>The updated user.</returns>
    [HttpPut("{id}/active-status")]
    [Authorize("Admin")]
    public async Task<IActionResult> UpdateUserActiveStatus(string id, [FromQuery] bool isActive)
    {
        try
        {
            var user = await dbService.UpdateUserActiveStatusAsync(id, isActive);
            return Ok(mapper.Map<UserDto>(user));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while updating user status" });
        }
    }
    
    /// <summary>
    /// Checks if the current user is an admin.
    /// </summary>
    /// <returns>True if the user is an admin, otherwise false.</returns>
    [HttpGet("is-admin")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> IsUserAdmin()
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        
        // Check if the user is an admin
        if (currentUser.Role != "Admin")
            return Forbid();
        
        return Ok(new { message = "User is an admin" });
    }
    
    /// <summary>
    /// Updates a user's personal information.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="model">The personal information to update.</param>
    /// <returns>The updated user information.</returns>
    [HttpPut("{id}/personal-info")]
    public async Task<IActionResult> UpdatePersonalInfo(string id, [FromBody] UserPersonalInfoModel model)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        
        // Only allow users to update their own personal info unless they are an admin
        if (currentUser.Id != id && currentUser.Role != "Admin")
            return Forbid();
        
        // Get existing user info or create new
        var existingInfo = await dbService.GetUserByIdAsync(id);
        UserPersonalInfo userInfo;
        
        if (existingInfo?.PersonalInfo == null)
        {
            // Create new personal info with basic details
            userInfo = new UserPersonalInfo
            {
                UserId = id,
                Address = model.Address ?? string.Empty,
                PhoneNumber = model.PhoneNumber ?? string.Empty,
                DateOfBirth = model.DateOfBirth ?? DateTime.UtcNow.ToLocalTime(),
                LegalFirstName = currentUser.DisplayName.Split(' ').FirstOrDefault() ?? string.Empty,
                LegalLastName = string.Join(" ", currentUser.DisplayName.Split(' ').Skip(1)) ?? string.Empty,
                City = "Not Specified",
                Country = "Not Specified"
            };
        }
        else
        {
            // Update existing info
            userInfo = existingInfo.PersonalInfo;
            userInfo.Address = model.Address ?? userInfo.Address;
            userInfo.PhoneNumber = model.PhoneNumber ?? userInfo.PhoneNumber;
            userInfo.DateOfBirth = model.DateOfBirth ?? userInfo.DateOfBirth;
        }
        
        var updatedInfo = await dbService.UpdateUserPersonalInfoAsync(id, userInfo);
        
        return Ok(mapper.Map<UserPersonalInfoDto>(updatedInfo));
    }
    
    /// <summary>
    /// Updates a user's preferences.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="preferences">The preferences to update.</param>
    /// <returns>The updated user preferences.</returns>
    [HttpPut("{id}/preferences")]
    public async Task<IActionResult> UpdatePreferences(string id, [FromBody] UserPreferencesDto preferences)
    {
        var currentUser = (User)HttpContext.Items["User"]!;
        
        // Only allow users to update their own preferences unless they are an admin
        if (currentUser.Id != id && currentUser.Role != "Admin")
            return Forbid();
            
        var userPreferences = new UserPreferences
        {
            RiskTolerance = preferences.RiskTolerance,
            InvestmentHorizon = preferences.InvestmentHorizon
        };
        
        var updatedPreferences = await dbService.UpdateUserPreferencesAsync(id, userPreferences);
        
        return Ok(mapper.Map<UserPreferencesDto>(updatedPreferences));
    }
}
