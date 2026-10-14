using System.Text.RegularExpressions;
using Platform.Identity.Application.Interfaces;

namespace Platform.Identity.Application.OrganizationManagement
{
    public sealed class OrganizationRoleNameBuilder : IOrganizationRoleNameBuilder
    {
        // Build full role name: {organizationId}_{slug}
        public string Build(string organizationId, string roleName)
        {
            var slug = BuildSlug(roleName);
            return string.IsNullOrWhiteSpace(organizationId) ? slug : $"{organizationId}_{slug}";
        }

        public string BuildSlug(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return string.Empty;

            // Normalize: lower-case, replace spaces and underscores with '-', remove invalid chars, collapse dashes
            var lower = roleName.Trim().ToLowerInvariant();
            // Replace spaces and underscores with dash
            lower = Regex.Replace(lower, @"[\u00A0\s_]+", "-");
            // Remove characters that are not letters, digits or dash
            lower = Regex.Replace(lower, "[^a-z0-9-]", string.Empty);
            // Collapse multiple dashes
            lower = Regex.Replace(lower, "-+", "-");
            // Trim dashes
            lower = lower.Trim('-');

            return lower;
        }
    }
}
