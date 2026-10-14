namespace Platform.Identity.Application.Identity
{
    public sealed class IdentityCreateUserRequest
    {
        public string Username { get; set; } = default!;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool Enabled { get; set; }
        public bool EmailVerified { get; set; }
    }
}
