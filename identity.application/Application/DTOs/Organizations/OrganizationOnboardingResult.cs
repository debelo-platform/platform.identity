namespace Platform.Identity.Application.DTOs.Organizations
{
    public sealed class OrganizationOnboardingResult
    {
        public string? OrganizationId { get; set; }
        public string? AdministratorId { get; set; }
        public string Status { get; set; } = "Completed";
        public string? FailedStage { get; set; }
        public string? Failure { get; set; }
    }
}
