namespace Platform.Identity.Infrastructure.Identity
{
    public interface IAdminTokenProvider
    {
        Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
        Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default);
    }
}
