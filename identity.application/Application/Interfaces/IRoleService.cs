using Platform.Identity.Application.DTOs.Roles;

namespace Platform.Identity.Application.Interfaces
{
    public interface IRoleService
    {
        Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
        Task<RoleDto?> GetByIdAsync(string roleId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RoleDto>> GetAsync(CancellationToken cancellationToken = default);
        Task UpdateAsync(string roleId, CreateRoleRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string roleId, CancellationToken cancellationToken = default);
    }
}
