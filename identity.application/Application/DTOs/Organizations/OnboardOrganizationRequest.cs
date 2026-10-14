using Platform.Identity.Application.DTOs.Users;

namespace Platform.Identity.Application.DTOs.Organizations
{
    public sealed class OnboardOrganizationRequest
    {
        public CreateOrganizationRequest Organization { get; set; } = new();
        public CreateUserRequest Administrator { get; set; } = new();
    }
}
