using System.Security.Claims;
using identity.api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moq;
using Platform.Identity.Application.DTOs.Users;
using Platform.Identity.Application.Interfaces;
using Xunit;

namespace identity.api.tests.Controllers;

public sealed class UsersControllerTests
{
    [Fact]
    public async Task Create_ReturnsCreatedUser()
    {
        var request = new CreateUserRequest { Username = "sample-user" };
        var user = new UserDto { Id = "user-1", Username = request.Username };
        var service = new Mock<IUserService>();
        service.Setup(s => s.CreateAsync(request, CancellationToken.None)).ReturnsAsync(user);

        var result = await CreateController(service).Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(UsersController.GetById), created.ActionName);
        Assert.Same(user, created.Value);
        service.Verify(s => s.CreateAsync(request, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Get_ReturnsUsersFromService()
    {
        IReadOnlyList<UserDto> users = new[]
        {
            new UserDto { Id = "user-1", Username = "sample-user" }
        };
        var service = new Mock<IUserService>();
        service.Setup(s => s.GetAsync(CancellationToken.None)).ReturnsAsync(users);

        var result = await CreateController(service).Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(users, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsUserWhenFound()
    {
        var user = new UserDto { Id = "user-1", Username = "sample-user" };
        var service = new Mock<IUserService>();
        service.Setup(s => s.GetByIdAsync("user-1", CancellationToken.None)).ReturnsAsync(user);

        var result = await CreateController(service).GetById("user-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(user, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenMissing()
    {
        var service = new Mock<IUserService>();
        service.Setup(s => s.GetByIdAsync("missing", CancellationToken.None)).ReturnsAsync((UserDto?)null);

        var result = await CreateController(service).GetById("missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_ForwardsRequestAndReturnsNoContent()
    {
        var request = new UpdateUserRequest { Email = "updated@example.com" };
        var service = new Mock<IUserService>();

        var result = await CreateController(service).Update("user-1", request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.UpdateAsync("user-1", request, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Delete_ForwardsUserIdAndReturnsNoContent()
    {
        var service = new Mock<IUserService>();

        var result = await CreateController(service).Delete("user-1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.DeleteAsync("user-1", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Enable_ForwardsUserIdAndReturnsNoContent()
    {
        var service = new Mock<IUserService>();

        var result = await CreateController(service).Enable("user-1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.EnableAsync("user-1", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Disable_ForwardsUserIdAndReturnsNoContent()
    {
        var service = new Mock<IUserService>();

        var result = await CreateController(service).Disable("user-1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.DisableAsync("user-1", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ForwardsRequestAndReturnsNoContent()
    {
        var request = new ResetPasswordRequest { Password = "new-password" };
        var service = new Mock<IUserService>();

        var result = await CreateController(service).ResetPassword("user-1", request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.ResetPasswordAsync("user-1", request, CancellationToken.None), Times.Once);
    }

    private static UsersController CreateController(Mock<IUserService> service)
    {
        var controller = new UsersController(service.Object);
        var routeData = new RouteData();
        routeData.Values["version"] = "1";
        controller.ControllerContext = new ControllerContext { RouteData = routeData };
        return controller;
    }
}
