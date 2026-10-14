using System.Security.Claims;
using System.Text.Json;
using identity.api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace identity.api.tests.Controllers;

public sealed class AccountControllerTests
{
    [Fact]
    public void GetCurrentUser_ReturnsProfileAndOrganizationClaimsWithoutTokenMetadata()
    {
        var controller = CreateController(
            new Claim("sub", "user-123"),
            new Claim("preferred_username", "sample-user"),
            new Claim("name", "Sample User"),
            new Claim("email", "sample@example.com"),
            new Claim("email_verified", "true"),
            new Claim("organization", "{\"dbl\":{\"id\":\"org-456\"}}"),
            new Claim("exp", "1791529815"),
            new Claim("jti", "token-id"));

        var result = Assert.IsType<OkObjectResult>(controller.GetCurrentUser());
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        var response = document.RootElement;

        Assert.Equal("user-123", response.GetProperty("id").GetString());
        Assert.Equal("sample-user", response.GetProperty("username").GetString());
        Assert.Equal("sample@example.com", response.GetProperty("email").GetString());
        Assert.True(response.GetProperty("email_verified").GetBoolean());
        Assert.Equal("org-456", response.GetProperty("organization").GetProperty("dbl").GetProperty("id").GetString());
        Assert.False(response.TryGetProperty("exp", out _));
        Assert.False(response.TryGetProperty("jti", out _));
    }

    [Fact]
    public void GetCurrentUser_WithoutSubjectReturnsUnauthorized()
    {
        var controller = CreateController(new Claim("email", "sample@example.com"));

        var result = controller.GetCurrentUser();

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static AccountController CreateController(params Claim[] claims)
    {
        var controller = new AccountController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "UnitTest"))
            }
        };

        return controller;
    }
}
