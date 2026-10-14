namespace Platform.Identity.Application.Identity
{
    public sealed class IdentityGroup
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Path { get; set; }
    }
}
