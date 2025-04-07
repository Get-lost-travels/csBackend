namespace WebApplication2.entities;

public partial class Notification
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Type { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public bool? IsRead { get; set; }

    public virtual User? User { get; set; }
}
