using Platform.Identity.Application.DTOs.Roles;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.Identity;
using Platform.Identity.Application.Exceptions;

namespace Platform.Identity.Application.OrganizationManagement
{
    public sealed class OrganizationRoleService : IOrganizationRoleService
    {
        private readonly IIdentityClient _identityClient;
        private readonly IOrganizationRoleNameBuilder _roleNameBuilder;

        public OrganizationRoleService(IIdentityClient identityClient, IOrganizationRoleNameBuilder roleNameBuilder)
        {
            _identityClient = identityClient;
            _roleNameBuilder = roleNameBuilder;
        }

        public async Task<RoleDto> CreateAsync(string organizationId, CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            // Build Identity role name using organization prefix
            var kcName = _roleNameBuilder.Build(organizationId, request.Name);

            var kcReq = new IdentityCreateRole
            {
                Name = kcName,
                Description = request.Description
            };

            var id = await _identityClient.CreateRoleAsync(kcReq, cancellationToken).ConfigureAwait(false);
            var created = await _identityClient.GetRoleAsync(id, cancellationToken).ConfigureAwait(false);
            if (created is null)
                throw new ResourceNotFoundException($"Role '{id}' was not found after creation.");

            return new RoleDto { Id = created.Id, Name = request.Name, Description = created.Description, OrganizationId = organizationId };
        }

        public async Task<RoleDto?> GetByIdAsync(string organizationId, string roleId, CancellationToken cancellationToken = default)
        {
            var role = await _identityClient.GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            if (role is null) return null;

            // Validate ownership: role.Name must start with {organizationId}_
            var prefix = string.IsNullOrEmpty(organizationId) ? string.Empty : organizationId + "_";
            if (!string.IsNullOrEmpty(prefix) && !role.Name.StartsWith(prefix))
                throw new ResourceNotFoundException($"Role '{roleId}' was not found in organization '{organizationId}'.");

            // Derive display name by removing prefix
            var displayName = role.Name;
            if (!string.IsNullOrEmpty(prefix) && displayName.StartsWith(prefix))
            {
                displayName = displayName.Substring(prefix.Length);
            }

            return new RoleDto { Id = role.Id, Name = displayName, Description = role.Description, OrganizationId = organizationId };
        }

        public Task<IReadOnlyList<RoleDto>> GetAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            // List roles and filter by organization prefix
            return GetOrganizationRolesInternalAsync(organizationId, cancellationToken);
        }

        private async Task<IReadOnlyList<RoleDto>> GetOrganizationRolesInternalAsync(string organizationId, CancellationToken cancellationToken)
        {
            var roles = await _identityClient.GetRolesAsync(cancellationToken).ConfigureAwait(false);
            var prefix = string.IsNullOrEmpty(organizationId) ? string.Empty : organizationId + "_";
            var result = new List<RoleDto>();

            foreach (var r in roles)
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    if (!r.Name.StartsWith(prefix))
                        continue;

                    var display = r.Name.Substring(prefix.Length);
                    result.Add(new RoleDto { Id = r.Id, Name = display, Description = r.Description, OrganizationId = organizationId });
                }
            }

            return result;
        }

        public async Task UpdateAsync(string organizationId, string roleId, CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            var existing = await _identityClient.GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                throw new ResourceNotFoundException($"Role '{roleId}' was not found.");

            var prefix = string.IsNullOrEmpty(organizationId) ? string.Empty : organizationId + "_";
            if (!string.IsNullOrEmpty(prefix) && !existing.Name.StartsWith(prefix))
                throw new ResourceNotFoundException($"Role '{roleId}' was not found in organization '{organizationId}'.");

            // Preserve the generated role name; do not allow changing prefix.
            var displayName = request.Name ?? existing.Name;
            var slug = _roleNameBuilder.BuildSlug(displayName);
            var newKcName = string.IsNullOrEmpty(prefix) ? slug : $"{prefix}{slug}";

            var update = new IdentityUpdateRole
            {
                Name = newKcName,
                Description = request.Description ?? existing.Description
            };

            await _identityClient.UpdateRoleAsync(roleId, update, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string organizationId, string roleId, CancellationToken cancellationToken = default)
        {
            var existing = await _identityClient.GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                throw new ResourceNotFoundException($"Role '{roleId}' was not found.");

            var prefix = string.IsNullOrEmpty(organizationId) ? string.Empty : organizationId + "_";
            if (!string.IsNullOrEmpty(prefix) && !existing.Name.StartsWith(prefix))
                throw new ResourceNotFoundException($"Role '{roleId}' was not found in organization '{organizationId}'.");

            await _identityClient.DeleteRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
        }

        public async Task AssignRoleToGroupAsync(string organizationId, string groupId, string roleId, CancellationToken cancellationToken = default)
        {
            // Validate role ownership
            var existing = await _identityClient.GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                throw new ResourceNotFoundException($"Role '{roleId}' was not found.");

            var prefix = string.IsNullOrEmpty(organizationId) ? string.Empty : organizationId + "_";
            if (!string.IsNullOrEmpty(prefix) && !existing.Name.StartsWith(prefix))
                throw new ResourceNotFoundException($"Role '{roleId}' was not found in organization '{organizationId}'.");

            await _identityClient.AssignRoleToOrganizationGroupAsync(organizationId, groupId, roleId, cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveRoleFromGroupAsync(string organizationId, string groupId, string roleId, CancellationToken cancellationToken = default)
        {
            // Validate role ownership
            var existing = await _identityClient.GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                throw new ResourceNotFoundException($"Role '{roleId}' was not found.");

            var prefix = string.IsNullOrEmpty(organizationId) ? string.Empty : organizationId + "_";
            if (!string.IsNullOrEmpty(prefix) && !existing.Name.StartsWith(prefix))
                throw new ResourceNotFoundException($"Role '{roleId}' was not found in organization '{organizationId}'.");

            await _identityClient.RemoveRoleFromOrganizationGroupAsync(organizationId, groupId, roleId, cancellationToken).ConfigureAwait(false);
        }
    }
}
