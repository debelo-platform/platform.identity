using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Platform.Identity.Application.DTOs.Roles;
using Platform.Identity.Application.Interfaces;

namespace identity.api.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/roles")]
    public sealed class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken cancellationToken)
        {
            var result = await _roleService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = result.Id, version = RouteData.Values["version"] }, result);
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var list = await _roleService.GetAsync(cancellationToken).ConfigureAwait(false);
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var role = await _roleService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (role is null) return NotFound();
            return Ok(role);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, CreateRoleRequest request, CancellationToken cancellationToken)
        {
            await _roleService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await _roleService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
    }
}
