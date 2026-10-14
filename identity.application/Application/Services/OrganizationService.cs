using Platform.Identity.Application.DTOs.Organizations;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.Exceptions;
using Platform.Identity.Application.Identity;

namespace Platform.Identity.Application.PlatformManagement
{
    public sealed class OrganizationService : IOrganizationService
    {
        private readonly IIdentityClient _identityClient;

        public OrganizationService(IIdentityClient identityClient)
        {
            _identityClient = identityClient;
        }

        public async Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default)
        {
            var orgReq = new IdentityCreateOrganization
            {
                Name = request.Name,
                Alias = request.Alias,
                Description = request.Description,
                Enabled = request.Enabled
            };

            var id = await _identityClient.CreateOrganizationAsync(orgReq, cancellationToken).ConfigureAwait(false);
            var created = await _identityClient.GetOrganizationAsync(id, cancellationToken).ConfigureAwait(false);
            return Map(created!);
        }

        public async Task<OrganizationDto?> GetByIdAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            var org = await _identityClient.GetOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
            return org is null ? null : Map(org);
        }

        public Task<IReadOnlyList<OrganizationDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            // List organizations from Identity
            return GetOrganizationsInternalAsync(cancellationToken);
        }

        public Task UpdateAsync(string organizationId, CreateOrganizationRequest request, CancellationToken cancellationToken = default)
        {
            var kcReq = new IdentityCreateOrganization
            {
                Name = request.Name,
                Alias = request.Alias,
                Description = request.Description,
                Enabled = request.Enabled
            };

            return _identityClient.UpdateOrganizationAsync(organizationId, kcReq, cancellationToken);
        }

        public Task DeleteAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            return _identityClient.DeleteOrganizationAsync(organizationId, cancellationToken);
        }

        public Task EnableAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            return SetEnabledAsync(organizationId, true, cancellationToken);
        }

        public Task DisableAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            return SetEnabledAsync(organizationId, false, cancellationToken);
        }

        public Task AddMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default)
        {
            return _identityClient.AddOrganizationMemberAsync(organizationId, userId, cancellationToken);
        }

        public Task RemoveMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default)
        {
            return _identityClient.RemoveOrganizationMemberAsync(organizationId, userId, cancellationToken);
        }

        public async Task<IReadOnlyList<Platform.Identity.Application.DTOs.Users.UserDto>> GetMembersAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            var members = await _identityClient.GetOrganizationMembersAsync(organizationId, cancellationToken).ConfigureAwait(false);
            return members.Select(m => new Platform.Identity.Application.DTOs.Users.UserDto
            {
                Id = m.Id,
                Username = m.Username,
                Email = m.Email,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Enabled = m.Enabled,
                EmailVerified = m.EmailVerified
            }).ToList();
        }

        private async Task<IReadOnlyList<OrganizationDto>> GetOrganizationsInternalAsync(CancellationToken cancellationToken)
        {
            var orgs = await _identityClient.GetOrganizationsAsync(cancellationToken).ConfigureAwait(false);
            return orgs.Select(Map).ToList();
        }

        private async Task SetEnabledAsync(string organizationId, bool enabled, CancellationToken cancellationToken)
        {
            var existing = await _identityClient.GetOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                throw new ResourceNotFoundException($"Organization '{organizationId}' was not found.");

            var update = new IdentityCreateOrganization
            {
                Name = existing.Name,
                Alias = existing.Alias,
                Description = existing.Description,
                Enabled = enabled
            };

            await _identityClient.UpdateOrganizationAsync(organizationId, update, cancellationToken).ConfigureAwait(false);
        }

        private static OrganizationDto Map(IdentityOrganization org)
        {
            return new OrganizationDto
            {
                Id = org.Id,
                Name = org.Name,
                Alias = org.Alias,
                Description = org.Description,
                Enabled = org.Enabled
            };
        }

        public async Task<int?> CountAsync(CancellationToken cancellation = default)
        {
            var count = await _identityClient.CountOrgAsync(cancellation);
            return count;
        }
        public async Task<long?> CountOrgMembersAsync(string organizationId, CancellationToken cancellation = default)
        {
            var count = await _identityClient.CountOrgMemberAsync(organizationId, cancellation);
            return count;
        }
    }
}
