namespace AnuradhapuraAI.Domain.Common;

public static class ApprovedRoleNames
{
    public const string RegisteredUser = "Registered User";
    public const string AgriculturalOfficer = "Agricultural Officer";
    public const string Administrator = "Administrator";

    public static readonly string[] AuthenticatedRoles =
    [
        RegisteredUser,
        AgriculturalOfficer,
        Administrator
    ];
}
