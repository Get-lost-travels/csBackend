namespace WebApplication2.entities;

public partial class UserSavedSearch
{
    public int Request { get; set; }

    public int? UserId { get; set; }

    public string Query { get; set; } = null!;

    public string? Filters { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
