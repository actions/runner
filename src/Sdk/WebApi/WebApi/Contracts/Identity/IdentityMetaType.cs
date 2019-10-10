
namespace GitHub.Services.Identity
{
    public enum IdentityMetaType
    {
        Member = 0,
        Guest = 1,
        CompanyAdministrator = 2,
        HelpdeskAdministrator = 3,
        Unknown = 255, // When the type isn't known (default value)
    }
}
