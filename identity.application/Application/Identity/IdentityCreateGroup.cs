namespace Platform.Identity.Application.Identity
{
    public sealed class IdentityCreateGroup
    {
        public string Name { get; set; } = default!;
        public string? Path { get; set; }
    }
}
