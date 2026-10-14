using Moq;
using Platform.Identity.Application.DTOs.Users;
using Platform.Identity.Application.Exceptions;
using Platform.Identity.Application.Identity;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.OrganizationManagement;
using Xunit;

namespace identity.api.tests.OrganizationManagement;

public sealed class OrganizationUserServiceTests
{
    [Fact]
    public async Task GetByIdAsync_RejectsUserOutsideOrganization()
    {
        var identityClient = new Mock<IIdentityClient>();
        var userService = new Mock<IUserService>();
        identityClient.Setup(client => client.IsOrganizationMemberAsync("org-1", "user-1", CancellationToken.None))
            .ReturnsAsync(false);
        var service = CreateService(identityClient, userService);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetByIdAsync("org-1", "user-1"));

        userService.Verify(client => client.GetByIdAsync(It.IsAny<string>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task AssignRoleAsync_RejectsRoleOwnedByAnotherOrganization()
    {
        var identityClient = new Mock<IIdentityClient>();
        identityClient.Setup(client => client.IsOrganizationMemberAsync("org-1", "user-1", CancellationToken.None))
            .ReturnsAsync(true);
        identityClient.Setup(client => client.GetRoleAsync("role-1", CancellationToken.None))
            .ReturnsAsync(new IdentityRole { Id = "role-1", Name = "org-2_admin" });
        var service = CreateService(identityClient);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.AssignRoleAsync("org-1", "user-1", "role-1"));

        identityClient.Verify(client => client.AssignRoleToUserAsync(It.IsAny<string>(), It.IsAny<IdentityRole>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task AssignRoleAsync_AssignsRoleOnlyAfterMembershipAndOwnershipChecks()
    {
        var identityClient = new Mock<IIdentityClient>();
        identityClient.Setup(client => client.IsOrganizationMemberAsync("org-1", "user-1", CancellationToken.None))
            .ReturnsAsync(true);
        identityClient.Setup(client => client.GetRoleAsync("role-1", CancellationToken.None))
            .ReturnsAsync(new IdentityRole { Id = "role-1", Name = "org-1_admin" });
        var service = CreateService(identityClient);

        await service.AssignRoleAsync("org-1", "user-1", "role-1");

        identityClient.Verify(client => client.AssignRoleToUserAsync(
            "user-1",
            It.Is<IdentityRole>(role => role.Id == "role-1" && role.Name == "org-1_admin"),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AlwaysUsesRequestedOrganization()
    {
        var identityClient = new Mock<IIdentityClient>();
        var userService = new Mock<IUserService>();
        var request = new CreateUserRequest { Username = "member", OrganizationId = "org-other" };
        userService.Setup(service => service.CreateAsync(It.Is<CreateUserRequest>(user =>
                user.Username == "member" && user.OrganizationId == "org-1"), CancellationToken.None))
            .ReturnsAsync(new UserDto { Id = "user-1", Username = "member" });
        var service = CreateService(identityClient, userService);

        var created = await service.CreateAsync("org-1", request);

        Assert.Equal("user-1", created.Id);
        userService.Verify(service => service.CreateAsync(It.Is<CreateUserRequest>(user => user.OrganizationId == "org-1"), CancellationToken.None), Times.Once);
    }

    private static OrganizationUserService CreateService(
        Mock<IIdentityClient> identityClient,
        Mock<IUserService>? userService = null)
    {
        return new OrganizationUserService(
            Mock.Of<IOrganizationService>(),
            userService?.Object ?? Mock.Of<IUserService>(),
            identityClient.Object,
            new OrganizationRoleNameBuilder());
    }
}
