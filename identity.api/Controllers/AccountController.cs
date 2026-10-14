using Asp.Versioning;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace identity.api.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/account")]
    public sealed class AccountController : ControllerBase
    {
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var subject = User.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject))
            {
                return Unauthorized();
            }

            var emailVerifiedValue = User.FindFirst("email_verified")?.Value;
            bool? emailVerified = bool.TryParse(emailVerifiedValue, out var parsedEmailVerified)
                ? parsedEmailVerified
                : null;

            return Ok(new
            {
                id = subject,
                username = User.FindFirst("preferred_username")?.Value,
                first_name = User.FindFirst("name")?.Value,
                middle_name = User.FindFirst("given_name")?.Value,
                last_name = User.FindFirst("family_name")?.Value,
                email = User.FindFirst("email")?.Value,
                email_verified = emailVerified,
                organization = GetOrganizationClaim(User)
            });
        }

        private static JsonElement? GetOrganizationClaim(ClaimsPrincipal user)
        {
            var organizationClaim = user.FindFirst("organization")?.Value;
            if (string.IsNullOrWhiteSpace(organizationClaim))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(organizationClaim);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
