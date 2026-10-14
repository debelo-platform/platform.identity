using Platform.Identity.Application.DTOs.Organizations;
using Platform.Identity.Application.DTOs.Users;
using Platform.Identity.Application.Interfaces;

namespace Platform.Identity.Application.PlatformManagement
{
    public sealed class OrganizationOnboardingService : IOrganizationOnboardingService
    {
        private readonly IOrganizationService _organizationService;
        private readonly IUserService _userService;
        private readonly IUserAccountActions _userAccountActions;

        public OrganizationOnboardingService(
            IOrganizationService organizationService,
            IUserService userService,
            IUserAccountActions userAccountActions)
        {
            _organizationService = organizationService;
            _userService = userService;
            _userAccountActions = userAccountActions;
        }

        public async Task<OrganizationOnboardingResult> OnboardAsync(
            OnboardOrganizationRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new OrganizationOnboardingResult();

            try
            {
                var organization = await _organizationService.CreateAsync(request.Organization, cancellationToken).ConfigureAwait(false);
                result.OrganizationId = organization.Id;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Failure(result, "OrganizationCreation", exception);
            }

            try
            {
                var administratorRequest = new CreateUserRequest
                {
                    Username = request.Administrator.Username,
                    Email = request.Administrator.Email,
                    FirstName = request.Administrator.FirstName,
                    LastName = request.Administrator.LastName,
                    Enabled = request.Administrator.Enabled,
                    EmailVerified = false
                };

                var administrator = await _userService.CreateAsync(administratorRequest, cancellationToken).ConfigureAwait(false);
                result.AdministratorId = administrator.Id;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Failure(result, "AdministratorCreation", exception);
            }

            try
            {
                await _organizationService.AddMemberAsync(result.OrganizationId!, result.AdministratorId!, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Failure(result, "AdministratorMembership", exception);
            }

            try
            {
                await _userAccountActions.SendAccountSetupEmailAsync(result.AdministratorId!, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Failure(result, "AccountSetupEmail", exception);
            }

            return result;
        }

        private static OrganizationOnboardingResult Failure(
            OrganizationOnboardingResult result,
            string stage,
            Exception exception)
        {
            result.Status = "PartiallyCompleted";
            result.FailedStage = stage;
            result.Failure = exception.Message;
            return result;
        }
    }
}
