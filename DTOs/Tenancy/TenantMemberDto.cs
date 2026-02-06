namespace erp.DTOs.Tenancy;

public record TenantMemberDto(
    int UserId,
    string Username,
    string? FullName,
    string? Email,
    bool IsDefault,
    DateTime CreatedAt,
    DateTime? RevokedAt
);
