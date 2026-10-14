namespace Platform.Identity.Infrastructure.Identity.Models
{
    public sealed class IdentityCreateOrganization
    {
        public string Name { get; set; } = default!;
        public string? Alias { get; set; }
        public string? Description { get; set; }
        public bool Enabled { get; set; }
    }
}
