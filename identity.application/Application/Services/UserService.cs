using Platform.Identity.Application.DTOs.Users;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.Exceptions;
using Platform.Identity.Application.Identity;

namespace Platform.Identity.Application.OrganizationManagement
{
    public sealed class UserService : IUserService
    {
        private readonly IIdentityClient _identityClient;

        public UserService(IIdentityClient identityClient)
        {
            _identityClient = identityClient;
        }

        public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            // Validate email format early to avoid sending invalid payloads to Identity
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                try
                {
                    // Use System.Net.Mail.MailAddress for basic RFC-style validation
                    _ = new System.Net.Mail.MailAddress(request.Email);
                }
                catch (FormatException)
                {
                    throw new ValidationException($"Email '{request.Email}' is not a valid email address.");
                }
            }

            var kcReq = new IdentityCreateUserRequest
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Enabled = request.Enabled,
                EmailVerified = request.EmailVerified
            };

            var id = await _identityClient.CreateUserAsync(kcReq, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(request.OrganizationId))
            {
                await _identityClient.AddOrganizationMemberAsync(request.OrganizationId!, id, cancellationToken).ConfigureAwait(false);
            }

            var created = await _identityClient.GetUserAsync(id, cancellationToken).ConfigureAwait(false);
            if (created is null)
                throw new ResourceNotFoundException($"User '{id}' was not found after creation.");

            return Map(created);
        }

        public async Task<UserDto?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _identityClient.GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return user is null ? null : Map(user);
        }

        public async Task<IReadOnlyList<UserDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            var users = await _identityClient.GetUsersAsync(cancellationToken).ConfigureAwait(false);
            return users.Select(Map).ToList();
        }

        public async Task UpdateAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var existing = await _identityClient.GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                throw new ResourceNotFoundException($"User '{userId}' was not found.");

            var update = new IdentityUpdateUser
            {
                Username = existing.Username,
                Email = request.Email ?? existing.Email,
                FirstName = request.FirstName ?? existing.FirstName,
                LastName = request.LastName ?? existing.LastName,
                Enabled = request.Enabled ?? existing.Enabled,
                EmailVerified = request.EmailVerified ?? existing.EmailVerified
            };

            await _identityClient.UpdateUserAsync(userId, update, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string userId, CancellationToken cancellationToken = default)
        {
            await _identityClient.DeleteUserAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        public async Task EnableAsync(string userId, CancellationToken cancellationToken = default)
        {
            await _identityClient.SetUserEnabledAsync(userId, true, cancellationToken).ConfigureAwait(false);
        }

        public async Task DisableAsync(string userId, CancellationToken cancellationToken = default)
        {
            await _identityClient.SetUserEnabledAsync(userId, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task ResetPasswordAsync(string userId, ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var kcReq = new IdentityResetPassword
            {
                Value = request.Password,
                Temporary = request.Temporary
            };

            await _identityClient.ResetPasswordAsync(userId, kcReq, cancellationToken).ConfigureAwait(false);
        }

        private static UserDto Map(IdentityUser user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Enabled = user.Enabled,
                EmailVerified = user.EmailVerified
            };
        }
    }
}
