namespace Platform.Identity.Application.Identity
{
    public sealed class IdentityCreateRole
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }
}
