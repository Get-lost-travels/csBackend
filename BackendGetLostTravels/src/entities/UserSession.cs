namespace WebApplication2.entities;

public partial class UserSession
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
