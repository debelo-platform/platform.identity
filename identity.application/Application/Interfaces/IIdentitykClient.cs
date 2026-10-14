using Platform.Identity.Application.DTOs.Users;
using Platform.Identity.Application.Identity;

namespace Platform.Identity.Application.Interfaces
{
    public interface IIdentityClient
    {
        //user
        Task<string> CreateUserAsync(IdentityCreateUserRequest request, CancellationToken cancellationToken = default);
        Task<IdentityUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<IdentityUser>> GetUsersAsync(CancellationToken cancellationToken = default);
        Task UpdateUserAsync(string userId, IdentityUpdateUser request, CancellationToken cancellationToken = default);
        Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
        Task SetUserEnabledAsync(string userId, bool enabled, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(string userId, IdentityResetPassword request, CancellationToken cancellationToken = default);
        Task SendExecuteActionsEmailAsync(string userId, IReadOnlyList<string> actions, CancellationToken cancellationToken = default);

        //organizations
        Task<string> CreateOrganizationAsync(IdentityCreateOrganization request, CancellationToken cancellationToken = default);
        Task<IdentityOrganization?> GetOrganizationAsync(string organizationId, CancellationToken cancellationToken = default);
        Task AddOrganizationMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
        Task<bool> IsOrganizationMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
        Task RemoveOrganizationMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<IdentityUser>> GetOrganizationMembersAsync(string organizationId, CancellationToken cancellationToken = default);
        Task<int?> CountOrgAsync(CancellationToken cancellationToken = default);
        Task<long?> CountOrgMemberAsync(string organizationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<IdentityOrganization>> GetOrganizationsAsync(CancellationToken cancellationToken = default);
        Task UpdateOrganizationAsync(string organizationId, IdentityCreateOrganization request, CancellationToken cancellationToken = default);
        Task DeleteOrganizationAsync(string organizationId, CancellationToken cancellationToken = default);

        // Roles
        Task<string> CreateRoleAsync(IdentityCreateRole request, CancellationToken cancellationToken = default);
        Task<IdentityRole?> GetRoleAsync(string roleId, CancellationToken cancellationToken = default);
        Task UpdateRoleAsync(string roleId, IdentityUpdateRole request, CancellationToken cancellationToken = default);
        Task DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<IdentityRole>> GetRolesAsync(CancellationToken cancellationToken = default);
        Task AssignRoleToUserAsync(string userId, IdentityRole role, CancellationToken cancellationToken = default);
        Task RemoveRoleFromUserAsync(string userId, IdentityRole role, CancellationToken cancellationToken = default);

        // Groups
        Task<string> CreateGroupAsync(IdentityCreateGroup request, CancellationToken cancellationToken = default);
        Task<IdentityGroup?> GetGroupAsync(string groupId, CancellationToken cancellationToken = default);
        Task UpdateGroupAsync(string groupId, IdentityUpdateGroup request, CancellationToken cancellationToken = default);
        Task DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default);
        Task AddGroupMemberAsync(string groupId, string userId, CancellationToken cancellationToken = default);
        Task RemoveGroupMemberAsync(string groupId, string userId, CancellationToken cancellationToken = default);
        Task AssignRoleToGroupAsync(string groupId, string roleId, CancellationToken cancellationToken = default);
        Task RemoveRoleFromGroupAsync(string groupId, string roleId, CancellationToken cancellationToken = default);
        Task AssignRoleToOrganizationGroupAsync(string organizationId, string groupId, string roleId, CancellationToken cancellationToken = default);
        Task RemoveRoleFromOrganizationGroupAsync(string organizationId, string groupId, string roleId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<IdentityUser>> GetGroupMembersAsync(string groupId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<IdentityGroup>> GetGroupsAsync(CancellationToken cancellationToken = default);

        // Organization-scoped groups
        Task<string> CreateOrganizationGroupAsync(string organizationId, IdentityCreateGroup request, CancellationToken cancellationToken = default);
        Task<IdentityGroup?> GetOrganizationGroupAsync(string organizationId, string groupId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<IdentityGroup>> GetOrganizationGroupsAsync(string organizationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<IdentityUser>> GetOrganizationGroupMembersAsync(string organizationId, string groupId, CancellationToken cancellationToken = default);
        Task UpdateOrganizationGroupAsync(string organizationId, string groupId, IdentityUpdateGroup request, CancellationToken cancellationToken = default);
        Task DeleteOrganizationGroupAsync(string organizationId, string groupId, CancellationToken cancellationToken = default);
        Task AddOrganizationGroupMemberAsync(string organizationId, string groupId, string userId, CancellationToken cancellationToken = default);
        Task RemoveOrganizationGroupMemberAsync(string organizationId, string groupId, string userId, CancellationToken cancellationToken = default);
    }
}
