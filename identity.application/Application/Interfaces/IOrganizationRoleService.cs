using Platform.Identity.Application.DTOs.Roles;

namespace Platform.Identity.Application.Interfaces
{
    public interface IOrganizationRoleService
    {
        Task<RoleDto> CreateAsync(string organizationId, CreateRoleRequest request, CancellationToken cancellationToken = default);
        Task<RoleDto?> GetByIdAsync(string organizationId, string roleId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RoleDto>> GetAsync(string organizationId, CancellationToken cancellationToken = default);
        Task UpdateAsync(string organizationId, string roleId, CreateRoleRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string organizationId, string roleId, CancellationToken cancellationToken = default);
        Task AssignRoleToGroupAsync(string organizationId, string groupId, string roleId, CancellationToken cancellationToken = default);
        Task RemoveRoleFromGroupAsync(string organizationId, string groupId, string roleId, CancellationToken cancellationToken = default);
    }
}
