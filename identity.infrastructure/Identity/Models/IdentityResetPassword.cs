namespace Platform.Identity.Infrastructure.Identity.Models
{
    public sealed class IdentityResetPassword
    {
        public string Type { get; set; } = "password";
        public string Value { get; set; } = default!;
        public bool Temporary { get; set; }
    }
}
