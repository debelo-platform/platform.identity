namespace Platform.Identity.Application.DTOs.Roles
{
    public sealed class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
