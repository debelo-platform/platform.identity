namespace Platform.Identity.Application.Identity
{
    public sealed class IdentityOrganization
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Alias { get; set; }
        public string? Description { get; set; }
        public bool Enabled { get; set; }
    }
}
