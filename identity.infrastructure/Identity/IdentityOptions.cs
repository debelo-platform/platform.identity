namespace Platform.Identity.Infrastructure.Identity
{
    using System.ComponentModel.DataAnnotations;

    public sealed class IdentityOptions
    {
        public const string SectionName = "IdentityProvider";

        [Required]
        public string BaseUrl { get; set; } = default!;

        [Required]
        public string Realm { get; set; } = "platform.debelo";

        [Required]
        public string ClientId { get; set; } = "debelo";
        public string? ClientSecret { get; set; } = "ShxxjZKYS9JMLwRyi0fBanG0InzbnbhY";

        // Admin client used by Platform.Identity to call the IDP Admin API
        [Required]
        public string AdminRealm { get; set; } = "master";
        public string AdminClientId { get; set; } = "admin-cli";
        public string? AdminUsername { get; set; } = "admin"; 
        public string? AdminPassword { get; set; } = "admin";
        public string? SwaggerRedirectUrl { get; set; }
    }
}
