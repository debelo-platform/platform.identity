namespace Platform.Identity.Application.DTOs.Organizations
{
    public sealed class OrganizationDto
    {
        public string Id { get; init; } = default!;
        public string Name { get; init; } = default!;
        public string? Alias { get; init; }
        public string? Description { get; init; }
        public bool Enabled { get; init; }
    }
}
