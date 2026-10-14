namespace Platform.Identity.Application.DTOs.Users
{
    public sealed class CreateUserRequest
    {
        public string Username { get; set; } = default!;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool Enabled { get; set; } = true;
        public bool EmailVerified { get; set; }
        public string? OrganizationId { get; set; }
    }
}
