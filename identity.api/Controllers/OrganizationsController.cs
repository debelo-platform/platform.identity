using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Platform.Identity.Application.DTOs.Organizations;
using Platform.Identity.Application.Interfaces;
using Platform.Identity.Application.DTOs.Roles;
using Platform.Identity.Application.DTOs.Groups;
using Platform.Identity.Application.DTOs.Users;

namespace identity.api.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/organizations")]
    public sealed class OrganizationsController : ControllerBase
    {
        private readonly IOrganizationService _organizationService;
        private readonly IOrganizationRoleService _organizationRoleService;
        private readonly IOrganizationGroupService _organizationGroupService;
        private readonly IOrganizationOnboardingService _organizationOnboardingService;
        private readonly IOrganizationUserService _organizationUserService;

        public OrganizationsController(IOrganizationService organizationService, IOrganizationRoleService organizationRoleService, IOrganizationGroupService organizationGroupService, IOrganizationOnboardingService organizationOnboardingService, IOrganizationUserService organizationUserService)
        {
            _organizationService = organizationService;
            _organizationRoleService = organizationRoleService;
            _organizationGroupService = organizationGroupService;
            _organizationOnboardingService = organizationOnboardingService;
            _organizationUserService = organizationUserService;
        }

        [HttpPost("{id}/users")]
        public async Task<IActionResult> CreateUser(string id, CreateUserRequest request, CancellationToken cancellationToken)
        {
            var user = await _organizationUserService.CreateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetUserById), new { id, userId = user.Id, version = RouteData.Values["version"] }, user);
        }

        [HttpGet("{id}/users")]
        public async Task<IActionResult> GetUsers(string id, CancellationToken cancellationToken)
        {
            var users = await _organizationUserService.GetAsync(id, cancellationToken).ConfigureAwait(false);
            return Ok(users);
        }

        [HttpGet("{id}/users/{userId}")]
        public async Task<IActionResult> GetUserById(string id, string userId, CancellationToken cancellationToken)
        {
            var user = await _organizationUserService.GetByIdAsync(id, userId, cancellationToken).ConfigureAwait(false);
            return user is null ? NotFound() : Ok(user);
        }

        [HttpPut("{id}/users/{userId}")]
        public async Task<IActionResult> UpdateUser(string id, string userId, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            await _organizationUserService.UpdateAsync(id, userId, request, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}/users/{userId}")]
        public async Task<IActionResult> DeleteUser(string id, string userId, CancellationToken cancellationToken)
        {
            await _organizationUserService.DeleteAsync(id, userId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/users/{userId}/enable")]
        public async Task<IActionResult> EnableUser(string id, string userId, CancellationToken cancellationToken)
        {
            await _organizationUserService.EnableAsync(id, userId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/users/{userId}/disable")]
        public async Task<IActionResult> DisableUser(string id, string userId, CancellationToken cancellationToken)
        {
            await _organizationUserService.DisableAsync(id, userId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/users/{userId}/password/reset")]
        public async Task<IActionResult> ResetUserPassword(string id, string userId, ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            await _organizationUserService.ResetPasswordAsync(id, userId, request, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}/users/{userId}/membership")]
        public async Task<IActionResult> RemoveUserMembership(string id, string userId, CancellationToken cancellationToken)
        {
            await _organizationUserService.RemoveMemberAsync(id, userId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/users/{userId}/roles/{roleId}")]
        public async Task<IActionResult> AssignRoleToUser(string id, string userId, string roleId, CancellationToken cancellationToken)
        {
            await _organizationUserService.AssignRoleAsync(id, userId, roleId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}/users/{userId}/roles/{roleId}")]
        public async Task<IActionResult> RemoveRoleFromUser(string id, string userId, string roleId, CancellationToken cancellationToken)
        {
            await _organizationUserService.RemoveRoleAsync(id, userId, roleId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpGet("{id}/groups")]
        public async Task<IActionResult> GetGroups(string id, CancellationToken cancellationToken)
        {
            var groups = await _organizationGroupService.GetAsync(id, cancellationToken).ConfigureAwait(false);
            return Ok(groups);
        }

        [HttpPost("onboarding")]
        public async Task<IActionResult> Onboard(OnboardOrganizationRequest request, CancellationToken cancellationToken)
        {
            var result = await _organizationOnboardingService.OnboardAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.Status != "Completed")
            {
                return StatusCode(StatusCodes.Status502BadGateway, result);
            }

            return CreatedAtAction(nameof(GetById), new { id = result.OrganizationId, version = RouteData.Values["version"] }, result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateOrganizationRequest request, CancellationToken cancellationToken)
        {
            var result = await _organizationService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = result.Id, version = RouteData.Values["version"] }, result);
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var list = await _organizationService.GetAsync(cancellationToken).ConfigureAwait(false);
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var org = await _organizationService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (org is null) return NotFound();
            return Ok(org);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, CreateOrganizationRequest request, CancellationToken cancellationToken)
        {
            await _organizationService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await _organizationService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/enable")]
        public async Task<IActionResult> Enable(string id, CancellationToken cancellationToken)
        {
            await _organizationService.EnableAsync(id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/disable")]
        public async Task<IActionResult> Disable(string id, CancellationToken cancellationToken)
        {
            await _organizationService.DisableAsync(id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/members/{userId}")]
        public async Task<IActionResult> AddMember(string id, string userId, CancellationToken cancellationToken)
        {
            await _organizationService.AddMemberAsync(id, userId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(string id, string userId, CancellationToken cancellationToken)
        {
            await _organizationService.RemoveMemberAsync(id, userId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(string id, CancellationToken cancellationToken)
        {
            var members = await _organizationService.GetMembersAsync(id, cancellationToken).ConfigureAwait(false);
            return Ok(members);
        }

        // Organization-scoped roles
        [HttpPost("{id}/roles")]
        public async Task<IActionResult> CreateRole(string id, CreateRoleRequest request, CancellationToken cancellationToken)
        {
            var result = await _organizationRoleService.CreateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetRoleById), new { id = id, roleId = result.Id, version = RouteData.Values["version"] }, result);
        }

        [HttpGet("{id}/roles")]
        public async Task<IActionResult> GetRoles(string id, CancellationToken cancellationToken)
        {
            var roles = await _organizationRoleService.GetAsync(id, cancellationToken).ConfigureAwait(false);
            return Ok(roles);
        }

        [HttpGet("{id}/roles/{roleId}")]
        public async Task<IActionResult> GetRoleById(string id, string roleId, CancellationToken cancellationToken)
        {
            var role = await _organizationRoleService.GetByIdAsync(id, roleId, cancellationToken).ConfigureAwait(false);
            if (role is null) return NotFound();
            return Ok(role);
        }

        [HttpPut("{id}/roles/{roleId}")]
        public async Task<IActionResult> UpdateRole(string id, string roleId, CreateRoleRequest request, CancellationToken cancellationToken)
        {
            await _organizationRoleService.UpdateAsync(id, roleId, request, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}/roles/{roleId}")]
        public async Task<IActionResult> DeleteRole(string id, string roleId, CancellationToken cancellationToken)
        {
            await _organizationRoleService.DeleteAsync(id, roleId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/groups")]
        public async Task<IActionResult> CreateGroup(string id, CreateGroupRequest request, CancellationToken cancellationToken)
        {
            var groupId = await _organizationGroupService.CreateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetGroupById), new { id = id, groupId = groupId, version = RouteData.Values["version"] }, null);
        }

        [HttpGet("{id}/groups/{groupId}")]
        public async Task<IActionResult> GetGroupById(string id, string groupId, CancellationToken cancellationToken)
        {
            var group = await _organizationGroupService.GetByIdAsync(id, groupId, cancellationToken).ConfigureAwait(false);
            if (group is null) return NotFound();
            return Ok(group);
        }

        [HttpPut("{id}/groups/{groupId}")]
        public async Task<IActionResult> UpdateGroup(string id, string groupId, CreateGroupRequest request, CancellationToken cancellationToken)
        {
            await _organizationGroupService.UpdateAsync(id, groupId, request, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}/groups/{groupId}")]
        public async Task<IActionResult> DeleteGroup(string id, string groupId, CancellationToken cancellationToken)
        {
            await _organizationGroupService.DeleteAsync(id, groupId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/groups/{groupId}/members/{userId}")]
        public async Task<IActionResult> AddGroupMember(string id, string groupId, string userId, CancellationToken cancellationToken)
        {
            await _organizationGroupService.AddMemberAsync(id, groupId, userId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}/groups/{groupId}/members/{userId}")]
        public async Task<IActionResult> RemoveGroupMember(string id, string groupId, string userId, CancellationToken cancellationToken)
        {
            await _organizationGroupService.RemoveMemberAsync(id, groupId, userId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpGet("{id}/groups/{groupId}/members")]
        public async Task<IActionResult> GetGroupMembers(string id, string groupId, CancellationToken cancellationToken)
        {
            var members = await _organizationGroupService.GetMembersAsync(id, groupId, cancellationToken).ConfigureAwait(false);
            return Ok(members);
        }

        [HttpPost("{id}/groups/{groupId}/roles/{roleId}")]
        public async Task<IActionResult> AssignRoleToGroup(string id, string groupId, string roleId, CancellationToken cancellationToken)
        {
            await _organizationRoleService.AssignRoleToGroupAsync(id, groupId, roleId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}/groups/{groupId}/roles/{roleId}")]
        public async Task<IActionResult> RemoveRoleFromGroup(string id, string groupId, string roleId, CancellationToken cancellationToken)
        {
            await _organizationRoleService.RemoveRoleFromGroupAsync(id, groupId, roleId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpGet("count")]
        public async Task<IActionResult> count(CancellationToken cancellationToken)
        {
            var orgcount = await _organizationService.CountAsync(cancellationToken).ConfigureAwait(false);
            if (orgcount is null) return NotFound();
            return Ok(orgcount);
        }

        [HttpGet("{id}/members/count")]
        public async Task<IActionResult> countMembers(string id, CancellationToken cancellationToken)
        {
            var orgcount = await _organizationService.CountOrgMembersAsync(id, cancellationToken).ConfigureAwait(false);
            if (orgcount is null) return NotFound();
            return Ok(orgcount);
        }
    }
}
