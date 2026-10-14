using Platform.Identity.Application.DTOs.Groups;

namespace Platform.Identity.Application.Interfaces
{
    public interface IGroupService
    {
        Task<GroupDto> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);
        Task<GroupDto?> GetByIdAsync(string groupId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GroupDto>> GetAsync(CancellationToken cancellationToken = default);
        Task UpdateAsync(string groupId, CreateGroupRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string groupId, CancellationToken cancellationToken = default);
        Task AddMemberAsync(string groupId, string userId, CancellationToken cancellationToken = default);
        Task RemoveMemberAsync(string groupId, string userId, CancellationToken cancellationToken = default);
        Task AssignRoleAsync(string groupId, string roleId, CancellationToken cancellationToken = default);
        Task RemoveRoleAsync(string groupId, string roleId, CancellationToken cancellationToken = default);
    }
}
