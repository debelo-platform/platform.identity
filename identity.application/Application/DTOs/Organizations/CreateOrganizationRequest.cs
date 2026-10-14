namespace Platform.Identity.Application.DTOs.Organizations
{
    public sealed class CreateOrganizationRequest
    {
        public string Name { get; set; } = default!;
        public string? Alias { get; set; }
        public string? Description { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
