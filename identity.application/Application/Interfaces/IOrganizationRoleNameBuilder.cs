namespace Platform.Identity.Application.Interfaces
{
    public interface IOrganizationRoleNameBuilder
    {
        string Build(string organizationId, string roleName);
        string BuildSlug(string roleName);
    }
}
