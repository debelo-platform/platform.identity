namespace Platform.Identity.Application.DTOs.Users
{
    public sealed class UserDto
    {
        public string Id { get; init; } = default!;
        public string Username { get; init; } = default!;
        public string? Email { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public bool Enabled { get; init; }
        public bool EmailVerified { get; init; }
    }
}
