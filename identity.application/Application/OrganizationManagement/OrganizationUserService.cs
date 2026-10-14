using Platform.Identity.Application.DTOs.Users;
using Platform.Identity.Application.Exceptions;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.Identity;

namespace Platform.Identity.Application.OrganizationManagement
{
    public sealed class OrganizationUserService : IOrganizationUserService
    {
        private readonly IOrganizationService _organizationService;
        private readonly IUserService _userService;
        private readonly IIdentityClient _identityClient;
        private readonly IOrganizationRoleNameBuilder _roleNameBuilder;

        public OrganizationUserService(
            IOrganizationService organizationService,
            IUserService userService,
            IIdentityClient identityClient,
            IOrganizationRoleNameBuilder roleNameBuilder)
        {
            _organizationService = organizationService;
            _userService = userService;
            _identityClient = identityClient;
            _roleNameBuilder = roleNameBuilder;
        }

        public async Task<UserDto> CreateAsync(string organizationId, CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            return await _userService.CreateAsync(new CreateUserRequest
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Enabled = request.Enabled,
                EmailVerified = request.EmailVerified,
                OrganizationId = organizationId
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<UserDto>> GetAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            return await _organizationService.GetMembersAsync(organizationId, cancellationToken).ConfigureAwait(false);
        }

        public async Task<UserDto?> GetByIdAsync(string organizationId, string userId, CancellationToken cancellationToken = default)
        {
            await EnsureMemberAsync(organizationId, userId, cancellationToken).ConfigureAwait(false);
            return await _userService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync(string organizationId, string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureMemberAsync(organizationId, userId, cancellationToken).ConfigureAwait(false);
            await _userService.UpdateAsync(userId, request, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string organizationId, string userId, CancellationToken cancellationToken = default)
        {
            await EnsureMemberAsync(organizationId, userId, cancellationToken).ConfigureAwait(false);
            await _userService.DeleteAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        public async Task EnableAsync(string organizationId, string userId, CancellationToken cancellationToken = default)
        {
            await EnsureMemberAsync(organizationId, userId, cancellationToken).ConfigureAwait(false);
            await _userService.EnableAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        public async Task DisableAsync(string organizationId, string userId, CancellationToken cancellationToken = default)
        {
            await EnsureMemberAsync(organizationId, userId, cancellationToken).ConfigureAwait(false);
            await _userService.DisableAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        public async Task ResetPasswordAsync(string organizationId, string userId, ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureMemberAsync(organizationId, userId, cancellationToken).ConfigureAwait(false);
            await _userService.ResetPasswordAsync(userId, request, cancellationToken).ConfigureAwait(false);
        }

        public Task RemoveMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default)
        {
            return _organizationService.RemoveMemberAsync(organizationId, userId, cancellationToken);
        }

        public async Task AssignRoleAsync(string organizationId, string userId, string roleId, CancellationToken cancellationToken = default)
        {
            await EnsureMemberAsync(organizationId, userId, cancellationToken).ConfigureAwait(false);
            var role = await GetOrganizationRoleAsync(organizationId, roleId, cancellationToken).ConfigureAwait(false);
            await _identityClient.AssignRoleToUserAsync(userId, role, cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveRoleAsync(string organizationId, string userId, string roleId, CancellationToken cancellationToken = default)
        {
            await EnsureMemberAsync(organizationId, userId, cancellationToken).ConfigureAwait(false);
            var role = await GetOrganizationRoleAsync(organizationId, roleId, cancellationToken).ConfigureAwait(false);
            await _identityClient.RemoveRoleFromUserAsync(userId, role, cancellationToken).ConfigureAwait(false);
        }

        private async Task EnsureMemberAsync(string organizationId, string userId, CancellationToken cancellationToken)
        {
            if (!await _identityClient.IsOrganizationMemberAsync(organizationId, userId, cancellationToken).ConfigureAwait(false))
            {
                throw new ResourceNotFoundException($"User '{userId}' was not found in organization '{organizationId}'.");
            }
        }

        private async Task<IdentityRole> GetOrganizationRoleAsync(string organizationId, string roleId, CancellationToken cancellationToken)
        {
            var role = await _identityClient.GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            var prefix = _roleNameBuilder.Build(organizationId, string.Empty);
            if (role is null || !role.Name.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new ResourceNotFoundException($"Role '{roleId}' was not found in organization '{organizationId}'.");
            }

            return role;
        }
    }
}
