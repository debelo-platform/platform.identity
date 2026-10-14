using Platform.Identity.Application.DTOs.Groups;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.Identity;

namespace Platform.Identity.Application.OrganizationManagement
{
    public sealed class OrganizationGroupService : IOrganizationGroupService
    {
        private readonly IIdentityClient _identityClient;

        public OrganizationGroupService(IIdentityClient IdentityClient)
        {
            _identityClient = IdentityClient;
        }

        public async Task<IReadOnlyList<GroupDto>> GetAsync(string organizationId, CancellationToken cancellationToken = default)
        {
            var groups = await _identityClient.GetOrganizationGroupsAsync(organizationId, cancellationToken).ConfigureAwait(false);
            return groups.Select(group => new GroupDto { Id = group.Id, Name = group.Name, Path = group.Path }).ToList();
        }

        public async Task<string> CreateAsync(string organizationId, CreateGroupRequest request, CancellationToken cancellationToken = default)
        {
            var kcReq = new IdentityCreateGroup
            {
                Name = request.Name,
                Path = request.Path
            };

            return await _identityClient.CreateOrganizationGroupAsync(organizationId, kcReq, cancellationToken).ConfigureAwait(false);
        }

        public async Task<GroupDto?> GetByIdAsync(string organizationId, string groupId, CancellationToken cancellationToken = default)
        {
            var group = await _identityClient.GetOrganizationGroupAsync(organizationId, groupId, cancellationToken).ConfigureAwait(false);
            if (group is null) return null;
            return new GroupDto { Id = group.Id, Name = group.Name, Path = group.Path };
        }

        public async Task UpdateAsync(string organizationId, string groupId, CreateGroupRequest request, CancellationToken cancellationToken = default)
        {
            var kcReq = new IdentityUpdateGroup
            {
                Name = request.Name,
                Path = request.Path
            };

            await _identityClient.UpdateOrganizationGroupAsync(organizationId, groupId, kcReq, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string organizationId, string groupId, CancellationToken cancellationToken = default)
        {
            await _identityClient.DeleteOrganizationGroupAsync(organizationId, groupId, cancellationToken).ConfigureAwait(false);
        }

        public async Task AddMemberAsync(string organizationId, string groupId, string userId, CancellationToken cancellationToken = default)
        {
            await _identityClient.AddOrganizationGroupMemberAsync(organizationId, groupId, userId, cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveMemberAsync(string organizationId, string groupId, string userId, CancellationToken cancellationToken = default)
        {
            await _identityClient.RemoveOrganizationGroupMemberAsync(organizationId, groupId, userId, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Platform.Identity.Application.DTOs.Users.UserDto>> GetMembersAsync(string organizationId, string groupId, CancellationToken cancellationToken = default)
        {
            var members = await _identityClient.GetOrganizationGroupMembersAsync(organizationId, groupId, cancellationToken).ConfigureAwait(false);
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
    }
}
