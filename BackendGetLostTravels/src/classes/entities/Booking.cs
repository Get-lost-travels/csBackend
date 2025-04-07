namespace WebApplication2.classes.entities;

public partial class Booking
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? ServiceId { get; set; }

    public DateTime? BookingDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual ETicket? ETicket { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<RefundDispute> RefundDisputes { get; set; } = new List<RefundDispute>();

    public virtual Service? Service { get; set; }

    public virtual User? User { get; set; }
}
