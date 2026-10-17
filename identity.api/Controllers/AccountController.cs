using Asp.Versioning;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace identity.api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/account")]
public sealed class AccountController : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var subject = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(subject))
            return Unauthorized();

        var firstName = User.FindFirst("given_name")?.Value;
        var lastName = User.FindFirst("family_name")?.Value;

        var displayName = User.FindFirst("name")?.Value;

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = string.Join( " ", new[] { firstName, lastName }.Where(name => !string.IsNullOrWhiteSpace(name)));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName =User.FindFirst("preferred_username")?.Value ?? subject;
        }

        var realmRoles = GetRoles(User, "realm_access");
        var clientRoles = GetRoles(User, "resource_access", "debelo");
        var organizations = GetOrganizations(User);

        var currentOrganizationId =User.FindFirst("current_organization_id")?.Value;

        return Ok(new
        {
            authenticated = true,

            user = new
            {
                id = subject,
                username = User.FindFirst("preferred_username")?.Value,
                firstName,
                lastName,
                displayName,
                email = User.FindFirst("email")?.Value,
                emailVerified = GetBooleanClaim(User, "email_verified")
            },

            roles = new
            {
                realm = realmRoles,
                client = clientRoles
            },

            organizations,
            currentOrganizationId
        });
    }

    private static string[] GetRoles(ClaimsPrincipal user, string claimName, string? clientId = null)
    {
        var claim = user.FindFirst(claimName)?.Value;

        if (string.IsNullOrWhiteSpace(claim))
            return [];

        try
        {
            using var document = JsonDocument.Parse(claim);
            var root = document.RootElement;

            if (clientId is not null)
            {
                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(clientId, out root))
                    return [];
            }

            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
                return [];

            return roles.EnumerateArray()
                .Where(role => role.ValueKind == JsonValueKind.String)
                .Select(role => role.GetString()!)
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string[] GetOrganizations(ClaimsPrincipal user)
    {
        return user.FindAll("organization")
            .SelectMany(claim => ParseOrganizationAliases(claim.Value))
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> ParseOrganizationAliases(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            yield break;

        // Keycloak organization alias, e.g. "ms".
        if (!value.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            yield return value;
            yield break;
        }

        string[] aliases;

        try
        {
            using var document = JsonDocument.Parse(value);

            aliases = document.RootElement.ValueKind == JsonValueKind.Array ? document.RootElement
                    .EnumerateArray()
                    .Where(element => element.ValueKind == JsonValueKind.String)
                    .Select(element => element.GetString())
                    .Where(alias => !string.IsNullOrWhiteSpace(alias))
                    .Select(alias => alias!)
                    .ToArray()
                : [];
        }
        catch (JsonException)
        {
            aliases = [];
        }

        foreach (var alias in aliases)
        {
            yield return alias;
        }
    }

    private static bool? GetBooleanClaim(ClaimsPrincipal user, string claimName)
    {
        var value = user.FindFirst(claimName)?.Value;

        return bool.TryParse(value, out var result) ? result : null;
    }
}
