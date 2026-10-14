namespace Platform.Identity.Application.Identity
{
    public sealed class IdentityRole
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }
}
