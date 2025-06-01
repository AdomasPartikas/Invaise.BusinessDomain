using Invaise.BusinessDomain.API.Attributes;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Invaise.BusinessDomain.Test.Unit.Attributes;

public class AuthorizeAttributeTests : TestBase
{
    private static AuthorizationFilterContext CreateContext(object? user = null, object? serviceAccount = null, bool includeAllowAnonymous = false)
    {
        // Create HttpContext with items
        var httpContext = new DefaultHttpContext();
        var items = new Dictionary<object, object?>();
        
        // Always add both keys to the dictionary to avoid KeyNotFoundException
        items["User"] = user;
        items["ServiceAccount"] = serviceAccount;
            
        httpContext.Items = items;
            
        // Create ActionDescriptor with endpoint metadata
        var actionDescriptor = new ActionDescriptor();
        if (includeAllowAnonymous)
        {
            actionDescriptor.EndpointMetadata = new[] { new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute() };
        }
        else
        {
            actionDescriptor.EndpointMetadata = Array.Empty<object>();
        }
        
        // Create FilterContext
        return new AuthorizationFilterContext(
            new ActionContext(
                httpContext,
                new RouteData(),
                actionDescriptor
            ),
            new List<IFilterMetadata>()
        );
    }
    
    [Fact]
    public void OnAuthorization_AllowAnonymous_DoesNothing()
    {
        // Arrange
        var attribute = new API.Attributes.AuthorizeAttribute();
        var context = CreateContext(includeAllowAnonymous: true);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        Assert.Null(context.Result);
    }
    
    [Fact]
    public void OnAuthorization_NoUserOrServiceAccount_Returns401Unauthorized()
    {
        // Arrange
        var attribute = new API.Attributes.AuthorizeAttribute();
        var context = CreateContext(user: null, serviceAccount: null);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        var result = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }
    
    [Fact]
    public void OnAuthorization_UserWithCorrectRole_Succeeds()
    {
        // Arrange
        var user = new User { Id = "user1", DisplayName = "Test User", Role = "Admin" };
        var attribute = new API.Attributes.AuthorizeAttribute("Admin");
        var context = CreateContext(user: user, serviceAccount: null);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        Assert.Null(context.Result);
    }
    
    [Fact]
    public void OnAuthorization_UserWithIncorrectRole_Returns403Forbidden()
    {
        // Arrange
        var user = new User { Id = "user1", DisplayName = "Test User", Role = "User" };
        var attribute = new API.Attributes.AuthorizeAttribute("Admin");
        var context = CreateContext(user: user, serviceAccount: null);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        var result = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }
    
    [Fact]
    public void OnAuthorization_ServiceAccountWithCorrectRole_Succeeds()
    {
        // Arrange
        var serviceAccount = new ServiceAccount { Id = "svc1", Name = "Test Service", Role = "Service" };
        var attribute = new API.Attributes.AuthorizeAttribute("Service");
        var context = CreateContext(user: null, serviceAccount: serviceAccount);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        Assert.Null(context.Result);
    }
    
    [Fact]
    public void OnAuthorization_ServiceAccountWithIncorrectRole_Returns403Forbidden()
    {
        // Arrange
        var serviceAccount = new ServiceAccount { Id = "svc1", Name = "Test Service", Role = "ReadOnly" };
        var attribute = new API.Attributes.AuthorizeAttribute("Service");
        var context = CreateContext(user: null, serviceAccount: serviceAccount);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        var result = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }
    
    [Fact]
    public void OnAuthorization_UserWithAnyRole_SucceedsWithNoRoleSpecified()
    {
        // Arrange
        var user = new User { Id = "user1", DisplayName = "Test User", Role = "User" };
        var attribute = new API.Attributes.AuthorizeAttribute();
        var context = CreateContext(user: user, serviceAccount: null);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        Assert.Null(context.Result);
    }
    
    [Fact]
    public void OnAuthorization_ServiceAccountWithRequiredPermissions_Succeeds()
    {
        // Arrange
        var serviceAccount = new ServiceAccount 
        { 
            Id = "svc1", 
            Name = "Test Service", 
            Role = "Service",
            Permissions = [ "read", "write" ]
        };
        
        var attribute = new API.Attributes.AuthorizeAttribute(new[] { "Service" }, new[] { "read" });
        var context = CreateContext(user: null, serviceAccount: serviceAccount);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        Assert.Null(context.Result);
    }
    
    [Fact]
    public void OnAuthorization_ServiceAccountWithMissingPermissions_Returns403Forbidden()
    {
        // Arrange
        var serviceAccount = new ServiceAccount 
        { 
            Id = "svc1", 
            Name = "Test Service", 
            Role = "Service",
            Permissions = ["read"]
        };
        
        var attribute = new API.Attributes.AuthorizeAttribute(["Service" ], [ "read", "write" ]);
        var context = CreateContext(user: null, serviceAccount: serviceAccount);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        var result = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }
    
    [Fact]
    public void OnAuthorization_UserWithPermissionsSpecified_IgnoresPermissionsCheck()
    {
        // Arrange
        var user = new User { Id = "user1", DisplayName = "Test User", Role = "Admin" };
        var attribute = new API.Attributes.AuthorizeAttribute(new[] { "Admin" }, new[] { "read", "write" });
        var context = CreateContext(user: user, serviceAccount: null);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        Assert.Null(context.Result);
    }
    
    [Fact]
    public void WithPermissions_CreatesAttributeWithPermissionsOnly()
    {
        // Arrange
        var serviceAccount = new ServiceAccount 
        { 
            Id = "svc1", 
            Name = "Test Service", 
            Role = "Service",
            Permissions = new string[] { "read", "write" }
        };
        
        var attribute = API.Attributes.AuthorizeAttribute.WithPermissions("read", "write");
        var context = CreateContext(user: null, serviceAccount: serviceAccount);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        Assert.Null(context.Result);
    }
    
    [Fact]
    public void WithPermissions_ServiceAccountWithMissingPermissions_Returns403Forbidden()
    {
        // Arrange
        var serviceAccount = new ServiceAccount 
        { 
            Id = "svc1", 
            Name = "Test Service", 
            Role = "Service",
            Permissions = new string[] { "read" }
        };
        
        var attribute = API.Attributes.AuthorizeAttribute.WithPermissions("read", "write");
        var context = CreateContext(user: null, serviceAccount: serviceAccount);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        var result = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }
    
    [Fact]
    public void OnAuthorization_MultipleRolesSpecified_UserWithOneRole_Succeeds()
    {
        // Arrange
        var user = new User { Id = "user1", DisplayName = "Test User", Role = "Admin" };
        var attribute = new API.Attributes.AuthorizeAttribute("Admin", "SuperAdmin");
        var context = CreateContext(user: user, serviceAccount: null);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        Assert.Null(context.Result);
    }
    
    [Fact]
    public void OnAuthorization_MultipleRolesSpecified_UserWithNoneOfRoles_Returns403Forbidden()
    {
        // Arrange
        var user = new User { Id = "user1", DisplayName = "Test User", Role = "User" };
        var attribute = new API.Attributes.AuthorizeAttribute("Admin", "SuperAdmin");
        var context = CreateContext(user: user, serviceAccount: null);
        
        // Act
        attribute.OnAuthorization(context);
        
        // Assert
        var result = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }
} 