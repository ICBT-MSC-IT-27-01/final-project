namespace AnuradhapuraAI.Domain.Entities;

public sealed class UserRole
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = [];
}
