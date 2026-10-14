using identity.api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moq;
using Platform.Identity.Application.DTOs.Roles;
using Platform.Identity.Application.Interfaces;
using Xunit;

namespace identity.api.tests.Controllers;

public sealed class RolesControllerTests
{
    [Fact]
    public async Task Create_ReturnsCreatedRole()
    {
        var request = new CreateRoleRequest { Name = "reader" };
        var role = new RoleDto { Id = "role-1", Name = request.Name };
        var service = new Mock<IRoleService>();
        service.Setup(s => s.CreateAsync(request, CancellationToken.None)).ReturnsAsync(role);

        var result = await CreateController(service).Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(RolesController.GetById), created.ActionName);
        Assert.Same(role, created.Value);
        service.Verify(s => s.CreateAsync(request, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Get_ReturnsRolesFromService()
    {
        IReadOnlyList<RoleDto> roles = new[]
        {
            new RoleDto { Id = "role-1", Name = "reader" }
        };
        var service = new Mock<IRoleService>();
        service.Setup(s => s.GetAsync(CancellationToken.None)).ReturnsAsync(roles);

        var result = await CreateController(service).Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(roles, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsRoleWhenFound()
    {
        var role = new RoleDto { Id = "role-1", Name = "reader" };
        var service = new Mock<IRoleService>();
        service.Setup(s => s.GetByIdAsync("role-1", CancellationToken.None)).ReturnsAsync(role);

        var result = await CreateController(service).GetById("role-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(role, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenMissing()
    {
        var service = new Mock<IRoleService>();
        service.Setup(s => s.GetByIdAsync("missing", CancellationToken.None)).ReturnsAsync((RoleDto?)null);

        var result = await CreateController(service).GetById("missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_ForwardsRequestAndReturnsNoContent()
    {
        var request = new CreateRoleRequest { Name = "writer" };
        var service = new Mock<IRoleService>();

        var result = await CreateController(service).Update("role-1", request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.UpdateAsync("role-1", request, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Delete_ForwardsRoleIdAndReturnsNoContent()
    {
        var service = new Mock<IRoleService>();

        var result = await CreateController(service).Delete("role-1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.DeleteAsync("role-1", CancellationToken.None), Times.Once);
    }

    private static RolesController CreateController(Mock<IRoleService> service)
    {
        var controller = new RolesController(service.Object);
        var routeData = new RouteData();
        routeData.Values["version"] = "1";
        controller.ControllerContext = new ControllerContext { RouteData = routeData };
        return controller;
    }
}
