using Moq;
using Platform.Identity.Application.DTOs.Organizations;
using Platform.Identity.Application.DTOs.Users;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.PlatformManagement;
using Xunit;

namespace identity.api.tests.PlatformManagement;

public sealed class OrganizationOnboardingServiceTests
{
    [Fact]
    public async Task OnboardAsync_CreatesOrganizationAdministratorMembershipAndSetupEmail()
    {
        var organizationService = new Mock<IOrganizationService>();
        var userService = new Mock<IUserService>();
        var accountActions = new Mock<IUserAccountActions>();
        var request = new OnboardOrganizationRequest
        {
            Organization = new CreateOrganizationRequest { Name = "Example" },
            Administrator = new CreateUserRequest { Username = "admin", Email = "admin@example.com", EmailVerified = true }
        };
        organizationService.Setup(service => service.CreateAsync(request.Organization, CancellationToken.None))
            .ReturnsAsync(new OrganizationDto { Id = "org-1", Name = "Example" });
        userService.Setup(service => service.CreateAsync(It.Is<CreateUserRequest>(user =>
                user.Username == "admin" && user.EmailVerified == false && user.OrganizationId == null), CancellationToken.None))
            .ReturnsAsync(new UserDto { Id = "user-1", Username = "admin" });

        var result = await CreateService(organizationService, userService, accountActions)
            .OnboardAsync(request);

        Assert.Equal("Completed", result.Status);
        Assert.Equal("org-1", result.OrganizationId);
        Assert.Equal("user-1", result.AdministratorId);
        organizationService.Verify(service => service.AddMemberAsync("org-1", "user-1", CancellationToken.None), Times.Once);
        accountActions.Verify(actions => actions.SendAccountSetupEmailAsync("user-1", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task OnboardAsync_ReturnsOrganizationIdWhenAdministratorCreationFails()
    {
        var organizationService = new Mock<IOrganizationService>();
        var userService = new Mock<IUserService>();
        var accountActions = new Mock<IUserAccountActions>();
        var request = new OnboardOrganizationRequest
        {
            Organization = new CreateOrganizationRequest { Name = "Example" },
            Administrator = new CreateUserRequest { Username = "admin" }
        };
        organizationService.Setup(service => service.CreateAsync(request.Organization, CancellationToken.None))
            .ReturnsAsync(new OrganizationDto { Id = "org-1", Name = "Example" });
        userService.Setup(service => service.CreateAsync(It.IsAny<CreateUserRequest>(), CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("user creation failed"));

        var result = await CreateService(organizationService, userService, accountActions)
            .OnboardAsync(request);

        Assert.Equal("PartiallyCompleted", result.Status);
        Assert.Equal("AdministratorCreation", result.FailedStage);
        Assert.Equal("org-1", result.OrganizationId);
        Assert.Null(result.AdministratorId);
        organizationService.Verify(service => service.AddMemberAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None), Times.Never);
        accountActions.Verify(actions => actions.SendAccountSetupEmailAsync(It.IsAny<string>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task OnboardAsync_ReturnsResourceIdsWhenMembershipFails()
    {
        var organizationService = new Mock<IOrganizationService>();
        var userService = new Mock<IUserService>();
        var accountActions = new Mock<IUserAccountActions>();
        var request = new OnboardOrganizationRequest
        {
            Organization = new CreateOrganizationRequest { Name = "Example" },
            Administrator = new CreateUserRequest { Username = "admin" }
        };
        organizationService.Setup(service => service.CreateAsync(request.Organization, CancellationToken.None))
            .ReturnsAsync(new OrganizationDto { Id = "org-1", Name = "Example" });
        userService.Setup(service => service.CreateAsync(It.IsAny<CreateUserRequest>(), CancellationToken.None))
            .ReturnsAsync(new UserDto { Id = "user-1", Username = "admin" });
        organizationService.Setup(service => service.AddMemberAsync("org-1", "user-1", CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("membership failed"));

        var result = await CreateService(organizationService, userService, accountActions)
            .OnboardAsync(request);

        Assert.Equal("PartiallyCompleted", result.Status);
        Assert.Equal("AdministratorMembership", result.FailedStage);
        Assert.Equal("org-1", result.OrganizationId);
        Assert.Equal("user-1", result.AdministratorId);
        accountActions.Verify(actions => actions.SendAccountSetupEmailAsync(It.IsAny<string>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task OnboardAsync_ReturnsResourceIdsWhenSetupEmailFails()
    {
        var organizationService = new Mock<IOrganizationService>();
        var userService = new Mock<IUserService>();
        var accountActions = new Mock<IUserAccountActions>();
        var request = new OnboardOrganizationRequest
        {
            Organization = new CreateOrganizationRequest { Name = "Example" },
            Administrator = new CreateUserRequest { Username = "admin" }
        };
        organizationService.Setup(service => service.CreateAsync(request.Organization, CancellationToken.None))
            .ReturnsAsync(new OrganizationDto { Id = "org-1", Name = "Example" });
        userService.Setup(service => service.CreateAsync(It.IsAny<CreateUserRequest>(), CancellationToken.None))
            .ReturnsAsync(new UserDto { Id = "user-1", Username = "admin" });
        accountActions.Setup(actions => actions.SendAccountSetupEmailAsync("user-1", CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("email failed"));

        var result = await CreateService(organizationService, userService, accountActions)
            .OnboardAsync(request);

        Assert.Equal("PartiallyCompleted", result.Status);
        Assert.Equal("AccountSetupEmail", result.FailedStage);
        Assert.Equal("org-1", result.OrganizationId);
        Assert.Equal("user-1", result.AdministratorId);
        organizationService.Verify(service => service.AddMemberAsync("org-1", "user-1", CancellationToken.None), Times.Once);
    }

    private static OrganizationOnboardingService CreateService(
        Mock<IOrganizationService> organizationService,
        Mock<IUserService> userService,
        Mock<IUserAccountActions> accountActions)
    {
        return new OrganizationOnboardingService(
            organizationService.Object,
            userService.Object,
            accountActions.Object);
    }
}
