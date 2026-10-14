using Microsoft.AspNetCore.Mvc.Controllers;
using Platform.Identity.Application.Interfaces;
using System.Security.Claims;

namespace identity.api.Middleware
{
    public sealed class OrganizationMembershipMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<OrganizationMembershipMiddleware> _logger;

        public OrganizationMembershipMiddleware(RequestDelegate next, ILogger<OrganizationMembershipMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IOrganizationService orgService)
        {
            var organizationIdObj = context.Request.RouteValues.TryGetValue("organizationId", out var explicitOrganizationId) ? explicitOrganizationId
                : string.Equals(
                    context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerName, "Organizations", StringComparison.Ordinal)
                    && context.Request.RouteValues.TryGetValue("id", out var organizationRouteId) ? organizationRouteId : null;

            if (organizationIdObj is null)
            {
                // no organization context on this route
                await _next(context).ConfigureAwait(false);
                return;
            }

            var orgId = organizationIdObj.ToString();
            if (string.IsNullOrWhiteSpace(orgId))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            // Ensure user is authenticated
            var user = context.User;
            if (user?.Identity is null || !user.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Unauthorized request for organization {OrgId} - unauthenticated user.", orgId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            // Get user id from token claims (nameidentifier, sub, user_id)
            string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? user.FindFirst("sub")?.Value ?? user.FindFirst("user_id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Authenticated request missing user id claim for organization {OrgId}.", orgId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            // Validate organization exists and is enabled
            var org = await orgService.GetByIdAsync(orgId, context.RequestAborted).ConfigureAwait(false);
            if (org is null)
            {
                _logger.LogWarning("Organization {OrgId} not found.", orgId);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (!org.Enabled)
            {
                _logger.LogWarning("Organization {OrgId} is disabled.", orgId);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            // Check membership
            try
            {
                var members = await orgService.GetMembersAsync(orgId, context.RequestAborted).ConfigureAwait(false);
                var isMember = members.Any(m => string.Equals(m.Id, userId, StringComparison.OrdinalIgnoreCase));
                if (!isMember)
                {
                    _logger.LogWarning("User {UserId} is not a member of organization {OrgId}.", userId, orgId);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate membership for user {UserId} in organization {OrgId}.", userId, orgId);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return;
            }

            // store organization context if downstream needs
            context.Items["OrganizationId"] = orgId;
            context.Items["OrganizationName"] = org.Name;

            await _next(context).ConfigureAwait(false);
        }
    }
}
