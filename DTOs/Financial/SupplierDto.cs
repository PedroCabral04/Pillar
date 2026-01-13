namespace erp.DTOs.Financial;

/// <summary>
/// DTO for creating a new supplier
/// </summary>
public class CreateSupplierDto
{
    public required string Name { get; set; }
    public string? TradeName { get; set; }
    public required string TaxId { get; set; }
    public string? StateRegistration { get; set; }
    public string? MunicipalRegistration { get; set; }

    // Address
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Country { get; set; } = "Brasil";

    // Contact
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    // Financial
    public int? CategoryId { get; set; }
    public decimal MinimumOrderValue { get; set; } = 0;
    public int DeliveryLeadTimeDays { get; set; } = 0;
    public int PaymentTermDays { get; set; } = 30;
    public string PaymentMethod { get; set; } = "Boleto";
    public bool IsPreferred { get; set; } = false;

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating a supplier
/// </summary>
public class UpdateSupplierDto : CreateSupplierDto
{
}

/// <summary>
/// DTO for supplier response
/// </summary>
public class SupplierDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? TradeName { get; set; }
    public required string TaxId { get; set; }
    public string? StateRegistration { get; set; }
    public string? MunicipalRegistration { get; set; }

    // Address
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Country { get; set; } = "Brasil";

    // Contact
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    // Financial
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal MinimumOrderValue { get; set; }
    public int DeliveryLeadTimeDays { get; set; }
    public int PaymentTermDays { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
}

/// <summary>
/// DTO for supplier summary (lists)
/// </summary>
public class SupplierSummaryDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? TradeName { get; set; }
    public required string TaxId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsPreferred { get; set; }
    public bool IsActive { get; set; }
}
