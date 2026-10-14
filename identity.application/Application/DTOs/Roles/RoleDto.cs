namespace Platform.Identity.Application.DTOs.Roles
{
    public sealed class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        // Optional organization id for organization-scoped roles
        public string? OrganizationId { get; set; }
    }
}
