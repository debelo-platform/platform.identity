using Platform.Identity.Application.DTOs.Groups;

namespace Platform.Identity.Application.Interfaces
{
    public interface IOrganizationGroupService
    {
        Task<string> CreateAsync(string organizationId, CreateGroupRequest request, CancellationToken cancellationToken = default);
        Task<GroupDto?> GetByIdAsync(string organizationId, string groupId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GroupDto>> GetAsync(string organizationId, CancellationToken cancellationToken = default);
        Task UpdateAsync(string organizationId, string groupId, CreateGroupRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string organizationId, string groupId, CancellationToken cancellationToken = default);
        Task AddMemberAsync(string organizationId, string groupId, string userId, CancellationToken cancellationToken = default);
        Task RemoveMemberAsync(string organizationId, string groupId, string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Platform.Identity.Application.DTOs.Users.UserDto>> GetMembersAsync(string organizationId, string groupId, CancellationToken cancellationToken = default);
    }
}
