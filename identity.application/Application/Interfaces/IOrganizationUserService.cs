using Platform.Identity.Application.DTOs.Users;

namespace Platform.Identity.Application.Interfaces
{
    public interface IOrganizationUserService
    {
        Task<UserDto> CreateAsync(string organizationId, CreateUserRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserDto>> GetAsync(string organizationId, CancellationToken cancellationToken = default);
        Task<UserDto?> GetByIdAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
        Task UpdateAsync(string organizationId, string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
        Task EnableAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
        Task DisableAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(string organizationId, string userId, ResetPasswordRequest request, CancellationToken cancellationToken = default);
        Task RemoveMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
        Task AssignRoleAsync(string organizationId, string userId, string roleId, CancellationToken cancellationToken = default);
        Task RemoveRoleAsync(string organizationId, string userId, string roleId, CancellationToken cancellationToken = default);
    }
}
