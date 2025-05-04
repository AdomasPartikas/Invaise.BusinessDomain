using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace Invaise.BusinessDomain.API.Controllers;

/// <summary>
/// Controller for handling authentication operations.
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{   
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="model">The registration data.</param>
    /// <returns>The authentication response with token and user information.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        try
        {
            var response = await authService.RegisterAsync(model);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }
    
    /// <summary>
    /// Authenticates a user.
    /// </summary>
    /// <param name="model">The login credentials.</param>
    /// <returns>The authentication response with token and user information.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        try
        {
            var response = await authService.LoginAsync(model);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Authenticates a user.
    /// </summary>
    /// <param name="model">The login credentials.</param>
    /// <returns>The authentication response with token and user information.</returns>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshModel model)
    {
        try
        {
            var response = await authService.RefreshToken(model);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("service/login")]
    [AllowAnonymous]
    public async Task<IActionResult> ServiceLogin([FromBody] ServiceLoginModel model)
    {
        try
        {
            var response = await authService.ServiceLoginAsync(model);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred during service login" });
        }
    }
    
    /// <summary>
    /// Handles forgot password requests by sending a temporary password to the user's email.
    /// </summary>
    /// <param name="model">The forgot password model containing the user's email.</param>
    /// <returns>A success message or error.</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
    {
        try
        {
            var result = await authService.ForgotPasswordAsync(model.Email);
            
            if (result)
            {
                return Ok(new { message = "If your email exists in our system, a password reset email has been sent." });
            }
            else
            {
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request." });
        }
    }
} 