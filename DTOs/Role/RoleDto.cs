namespace erp.DTOs.Role;

public class RoleDto {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Abbreviation { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not RoleDto other) return false;
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return Abbreviation;
    }
}
