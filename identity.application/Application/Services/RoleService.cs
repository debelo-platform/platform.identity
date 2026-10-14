using Platform.Identity.Application.DTOs.Roles;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.Exceptions;
using Platform.Identity.Application.Identity;

namespace Platform.Identity.Application.OrganizationManagement
{
    public sealed class RoleService : IRoleService
    {
        private readonly IIdentityClient _IdentityClient;

        public RoleService(IIdentityClient identityClient)
        {
            _IdentityClient = identityClient;
        }

        public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            var kcReq = new IdentityCreateRole
            {
                Name = request.Name,
                Description = request.Description
            };

            var id = await _IdentityClient.CreateRoleAsync(kcReq, cancellationToken).ConfigureAwait(false);

            var created = await _IdentityClient.GetRoleAsync(id, cancellationToken).ConfigureAwait(false);
            if (created is null)
                throw new ResourceNotFoundException($"Role '{id}' was not found after creation.");

            return new RoleDto { Id = created.Id, Name = created.Name, Description = created.Description };
        }

        public async Task<RoleDto?> GetByIdAsync(string roleId, CancellationToken cancellationToken = default)
        {
            var role = await _IdentityClient.GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            return role is null ? null : new RoleDto { Id = role.Id, Name = role.Name, Description = role.Description };
        }

        public async Task<IReadOnlyList<RoleDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _IdentityClient.GetRolesAsync(cancellationToken).ConfigureAwait(false);
            return roles.Select(role => new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            }).ToList();
        }

        public async Task UpdateAsync(string roleId, CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            var existing = await _IdentityClient.GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                throw new ResourceNotFoundException($"Role '{roleId}' was not found.");

            var update = new IdentityUpdateRole
            {
                Name = request.Name ?? existing.Name,
                Description = request.Description ?? existing.Description
            };

            await _IdentityClient.UpdateRoleAsync(roleId, update, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string roleId, CancellationToken cancellationToken = default)
        {
            await _IdentityClient.DeleteRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
        }
    }
}
