using identity.api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moq;
using Platform.Identity.Application.DTOs.Groups;
using Platform.Identity.Application.Interfaces;
using Xunit;

namespace identity.api.tests.Controllers;

public sealed class GroupsControllerTests
{
    [Fact]
    public async Task Create_ReturnsCreatedGroup()
    {
        var request = new CreateGroupRequest { Name = "readers" };
        var group = new GroupDto { Id = "group-1", Name = request.Name };
        var service = new Mock<IGroupService>();
        service.Setup(s => s.CreateAsync(request, CancellationToken.None)).ReturnsAsync(group);

        var result = await CreateController(service).Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(GroupsController.GetById), created.ActionName);
        Assert.Same(group, created.Value);
        service.Verify(s => s.CreateAsync(request, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsGroupWhenFound()
    {
        var group = new GroupDto { Id = "group-1", Name = "readers" };
        var service = new Mock<IGroupService>();
        service.Setup(s => s.GetByIdAsync("group-1", CancellationToken.None)).ReturnsAsync(group);

        var result = await CreateController(service).GetById("group-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(group, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWhenMissing()
    {
        var service = new Mock<IGroupService>();
        service.Setup(s => s.GetByIdAsync("missing", CancellationToken.None)).ReturnsAsync((GroupDto?)null);

        var result = await CreateController(service).GetById("missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddMember_ForwardsIdsAndReturnsNoContent()
    {
        var service = new Mock<IGroupService>();

        var result = await CreateController(service).AddMember("group-1", "user-1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.AddMemberAsync("group-1", "user-1", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RemoveMember_ForwardsIdsAndReturnsNoContent()
    {
        var service = new Mock<IGroupService>();

        var result = await CreateController(service).RemoveMember("group-1", "user-1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.RemoveMemberAsync("group-1", "user-1", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task AssignRole_ForwardsIdsAndReturnsNoContent()
    {
        var service = new Mock<IGroupService>();

        var result = await CreateController(service).AssignRole("group-1", "role-1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.AssignRoleAsync("group-1", "role-1", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RemoveRole_ForwardsIdsAndReturnsNoContent()
    {
        var service = new Mock<IGroupService>();

        var result = await CreateController(service).RemoveRole("group-1", "role-1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.RemoveRoleAsync("group-1", "role-1", CancellationToken.None), Times.Once);
    }

    private static GroupsController CreateController(Mock<IGroupService> service)
    {
        var controller = new GroupsController(service.Object);
        var routeData = new RouteData();
        routeData.Values["version"] = "1";
        controller.ControllerContext = new ControllerContext { RouteData = routeData };
        return controller;
    }
}
