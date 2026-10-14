using Platform.Identity.Application.Interfaces;

namespace Platform.Identity.Application.OrganizationManagement
{
    public sealed class UserAccountActions : IUserAccountActions
    {
        private readonly IIdentityClient _identityClient;

        public UserAccountActions(IIdentityClient identityClient)
        {
            _identityClient = identityClient;
        }

        public Task SendAccountSetupEmailAsync(string userId, CancellationToken cancellationToken = default)
        {
            return _identityClient.SendExecuteActionsEmailAsync(
                userId,
                new[] { "VERIFY_EMAIL", "UPDATE_PASSWORD" },
                cancellationToken);
        }
    }
}
