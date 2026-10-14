namespace Platform.Identity.Application.DTOs.Groups
{
    public sealed class CreateGroupRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Path { get; set; }
    }
}
