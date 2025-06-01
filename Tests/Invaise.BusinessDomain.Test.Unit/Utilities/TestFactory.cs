namespace Invaise.BusinessDomain.Test.Unit.Utilities;

/// <summary>
/// Utility class to help generate test data and standardized tests
/// </summary>
public static class TestFactory
{
    /// <summary>
    /// Creates a new AutoFixture instance configured with AutoMoq
    /// </summary>
    public static IFixture CreateFixture()
    {
        return new Fixture().Customize(new AutoMoqCustomization());
    }
    
    /// <summary>
    /// Creates a standard OkResult test for a controller
    /// </summary>
    public static void AssertOkResult(IActionResult result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }
    
    /// <summary>
    /// Creates a standard BadRequest test for a controller
    /// </summary>
    public static void AssertBadRequestResult(IActionResult result, string? expectedMessage = null!)
    {
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        
        if (expectedMessage != null)
        {
            dynamic value = badRequestResult.Value;
            Assert.Equal(expectedMessage, value.message.ToString());
        }
    }
    
    /// <summary>
    /// Creates a standard NotFound test for a controller
    /// </summary>
    public static void AssertNotFoundResult(IActionResult result)
    {
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }
    
    /// <summary>
    /// Creates a standard InternalServerError test for a controller
    /// </summary>
    public static void AssertInternalServerErrorResult(IActionResult result)
    {
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
    
    /// <summary>
    /// Helper method to handle response with anonymous types
    /// </summary>
    public static void AssertResponseMessage(IActionResult result, string expectedMessage)
    {
        if (result is OkObjectResult okResult)
        {
            var messageProperty = okResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal(expectedMessage, messageProperty.GetValue(okResult.Value).ToString());
        }
        else if (result is BadRequestObjectResult badRequestResult)
        {
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal(expectedMessage, messageProperty.GetValue(badRequestResult.Value).ToString());
        }
        else if (result is ObjectResult objectResult)
        {
            var messageProperty = objectResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal(expectedMessage, messageProperty.GetValue(objectResult.Value).ToString());
        }
        else
        {
            Assert.Fail($"Result type {result.GetType().Name} is not supported for message assertion");
        }
    }
} 