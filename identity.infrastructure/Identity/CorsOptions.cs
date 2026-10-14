namespace identity.infrastructure.Identity
{
    public sealed class CorsOptions
    {
        public string[] AllowedOrigins { get; init; } = [];
        public bool AllowCredentials { get; init; }
    }
}
