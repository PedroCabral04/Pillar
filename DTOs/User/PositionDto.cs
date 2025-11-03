namespace erp.DTOs.User;

public class PositionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Code { get; set; }
    public int? Level { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public int? DefaultDepartmentId { get; set; }
    public string? DefaultDepartmentName { get; set; }
    public string? Requirements { get; set; }
    public string? Responsibilities { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
}

public class CreatePositionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Code { get; set; }
    public int? Level { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public int? DefaultDepartmentId { get; set; }
    public string? Requirements { get; set; }
    public string? Responsibilities { get; set; }
}

public class UpdatePositionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Code { get; set; }
    public int? Level { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public int? DefaultDepartmentId { get; set; }
    public string? Requirements { get; set; }
    public string? Responsibilities { get; set; }
    public bool IsActive { get; set; }
}
