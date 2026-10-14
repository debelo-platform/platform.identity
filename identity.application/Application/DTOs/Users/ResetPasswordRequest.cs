namespace Platform.Identity.Application.DTOs.Users
{
    public sealed class ResetPasswordRequest
    {
        public string Password { get; set; } = default!;
        public bool Temporary { get; set; } = true;
    }
}
