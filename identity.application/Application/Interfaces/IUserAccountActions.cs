namespace Platform.Identity.Application.Interfaces
{
    public interface IUserAccountActions
    {
        Task SendAccountSetupEmailAsync(string userId, CancellationToken cancellationToken = default);
    }
}
