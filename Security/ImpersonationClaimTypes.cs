namespace erp.Security;

public static class ImpersonationClaimTypes
{
    public const string IsImpersonating = "pillar/impersonation/is_impersonating";
    public const string ImpersonatorUserId = "pillar/impersonation/original_user_id";
    public const string ImpersonatorUserName = "pillar/impersonation/original_user_name";
}
