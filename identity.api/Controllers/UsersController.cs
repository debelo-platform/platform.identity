using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Platform.Identity.Application.DTOs.Users;
using Platform.Identity.Application.Interfaces;

namespace identity.api.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/users")]
    public sealed class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
        {
            var result = await _userService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = result.Id, version = RouteData.Values["version"] }, result);
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var list = await _userService.GetAsync(cancellationToken).ConfigureAwait(false);
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user is null) return NotFound();
            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            await _userService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await _userService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/enable")]
        public async Task<IActionResult> Enable(string id, CancellationToken cancellationToken)
        {
            await _userService.EnableAsync(id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/disable")]
        public async Task<IActionResult> Disable(string id, CancellationToken cancellationToken)
        {
            await _userService.DisableAsync(id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/password/reset")]
        public async Task<IActionResult> ResetPassword(string id, ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            await _userService.ResetPasswordAsync(id, request, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
    }
}
