namespace Platform.Identity.Infrastructure.Identity.Models
{
    public sealed class AuthenticationCreateUser
    {
        public string Username { get; set; } = default!;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool Enabled { get; set; }
        public bool EmailVerified { get; set; }
    }
}
