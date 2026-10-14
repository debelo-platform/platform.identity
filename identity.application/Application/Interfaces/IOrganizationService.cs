using Platform.Identity.Application.DTOs.Organizations;

namespace Platform.Identity.Application.Interfaces
{
    public interface IOrganizationService
    {
        Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default);
        Task<OrganizationDto?> GetByIdAsync(string organizationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<OrganizationDto>> GetAsync(CancellationToken cancellationToken = default);
        Task UpdateAsync(string organizationId, CreateOrganizationRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string organizationId, CancellationToken cancellationToken = default);
        Task EnableAsync(string organizationId, CancellationToken cancellationToken = default);
        Task DisableAsync(string organizationId, CancellationToken cancellationToken = default);
        Task AddMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
        Task RemoveMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DTOs.Users.UserDto>> GetMembersAsync(string organizationId, CancellationToken cancellationToken = default);
        Task<int?> CountAsync(CancellationToken cancellation = default);
        Task<long?> CountOrgMembersAsync(string organizationId, CancellationToken cancellation = default);
    }
}
