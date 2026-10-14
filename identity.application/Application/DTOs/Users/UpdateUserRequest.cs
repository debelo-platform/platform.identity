namespace Platform.Identity.Application.DTOs.Users
{
    public sealed class UpdateUserRequest
    {
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? Enabled { get; set; }
        public bool? EmailVerified { get; set; }
    }
}
