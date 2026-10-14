using Platform.Identity.Application.DTOs.Organizations;

namespace Platform.Identity.Application.Interfaces
{
    public interface IOrganizationOnboardingService
    {
        Task<OrganizationOnboardingResult> OnboardAsync(OnboardOrganizationRequest request, CancellationToken cancellationToken = default);
    }
}
