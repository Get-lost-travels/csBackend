namespace WebApplication2.classes.entities;

public partial class PaymentMethod
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Provider { get; set; } = null!;

    public string Details { get; set; } = null!;

    public bool? IsDefault { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
