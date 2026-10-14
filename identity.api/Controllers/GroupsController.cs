using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Platform.Identity.Application.DTOs.Groups;
using Platform.Identity.Application.Interfaces;

namespace identity.api.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/groups")]
    public sealed class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateGroupRequest request, CancellationToken cancellationToken)
        {
            var result = await _groupService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = result.Id, version = RouteData.Values["version"] }, result);
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var groups = await _groupService.GetAsync(cancellationToken).ConfigureAwait(false);
            return Ok(groups);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var group = await _groupService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (group is null) return NotFound();
            return Ok(group);
        }

        [HttpPost("{id}/members/{userId}")]
        public async Task<IActionResult> AddMember(string id, string userId, CancellationToken cancellationToken)
        {
            await _groupService.AddMemberAsync(id, userId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(string id, string userId, CancellationToken cancellationToken)
        {
            await _groupService.RemoveMemberAsync(id, userId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/roles/{roleId}")]
        public async Task<IActionResult> AssignRole(string id, string roleId, CancellationToken cancellationToken)
        {
            await _groupService.AssignRoleAsync(id, roleId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}/roles/{roleId}")]
        public async Task<IActionResult> RemoveRole(string id, string roleId, CancellationToken cancellationToken)
        {
            await _groupService.RemoveRoleAsync(id, roleId, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
    }
}
