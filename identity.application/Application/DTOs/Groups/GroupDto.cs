namespace Platform.Identity.Application.DTOs.Groups
{
    public sealed class GroupDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Path { get; set; }
    }
}
