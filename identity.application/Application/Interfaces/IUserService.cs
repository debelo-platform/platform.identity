using Platform.Identity.Application.DTOs.Users;

namespace Platform.Identity.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
        Task<UserDto?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserDto>> GetAsync(CancellationToken cancellationToken = default);
        Task UpdateAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string userId, CancellationToken cancellationToken = default);
        Task EnableAsync(string userId, CancellationToken cancellationToken = default);
        Task DisableAsync(string userId, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(string userId, ResetPasswordRequest request, CancellationToken cancellationToken = default);
    }
}
