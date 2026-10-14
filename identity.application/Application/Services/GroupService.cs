using Platform.Identity.Application.DTOs.Groups;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.Identity;
using Platform.Identity.Application.Exceptions;

namespace Platform.Identity.Application.OrganizationManagement
{
    public sealed class GroupService : IGroupService
    {
        private readonly IIdentityClient _IdentityClient;

        public GroupService(IIdentityClient identityClient)
        {
            _IdentityClient = identityClient;
        }

        public async Task<GroupDto> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken = default)
        {
            var kcReq = new IdentityCreateGroup
            {
                Name = request.Name,
                Path = request.Path
            };

            var id = await _IdentityClient.CreateGroupAsync(kcReq, cancellationToken).ConfigureAwait(false);

            var created = await _IdentityClient.GetGroupAsync(id, cancellationToken).ConfigureAwait(false);
            if (created is null)
                throw new ResourceNotFoundException($"Group '{id}' was not found after creation.");

            return new GroupDto { Id = created.Id, Name = created.Name, Path = created.Path };
        }

        public async Task<GroupDto?> GetByIdAsync(string groupId, CancellationToken cancellationToken = default)
        {
            var group = await _IdentityClient.GetGroupAsync(groupId, cancellationToken).ConfigureAwait(false);
            return group is null ? null : new GroupDto { Id = group.Id, Name = group.Name, Path = group.Path };
        }

        public async Task<IReadOnlyList<GroupDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            var groups = await _IdentityClient.GetGroupsAsync(cancellationToken).ConfigureAwait(false);
            return groups.Select(group => new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Path = group.Path
            }).ToList();
        }

        public async Task UpdateAsync(string groupId, CreateGroupRequest request, CancellationToken cancellationToken = default)
        {
            var existing = await _IdentityClient.GetGroupAsync(groupId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                throw new ResourceNotFoundException($"Group '{groupId}' was not found.");

            var update = new IdentityUpdateGroup
            {
                Name = request.Name ?? existing.Name,
                Path = request.Path ?? existing.Path
            };

            await _IdentityClient.UpdateGroupAsync(groupId, update, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string groupId, CancellationToken cancellationToken = default)
        {
            await _IdentityClient.DeleteGroupAsync(groupId, cancellationToken).ConfigureAwait(false);
        }

        public async Task AddMemberAsync(string groupId, string userId, CancellationToken cancellationToken = default)
        {
            await _IdentityClient.AddGroupMemberAsync(groupId, userId, cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveMemberAsync(string groupId, string userId, CancellationToken cancellationToken = default)
        {
            await _IdentityClient.RemoveGroupMemberAsync(groupId, userId, cancellationToken).ConfigureAwait(false);
        }

        public async Task AssignRoleAsync(string groupId, string roleId, CancellationToken cancellationToken = default)
        {
            await _IdentityClient.AssignRoleToGroupAsync(groupId, roleId, cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveRoleAsync(string groupId, string roleId, CancellationToken cancellationToken = default)
        {
            await _IdentityClient.RemoveRoleFromGroupAsync(groupId, roleId, cancellationToken).ConfigureAwait(false);
        }
    }
}
