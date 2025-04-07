namespace WebApplication2.entities;

public partial class Payment
{
    public int Id { get; set; }

    public int? BookingId { get; set; }

    public double Amount { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual Booking? Booking { get; set; }

    public virtual ICollection<RefundDispute> RefundDisputes { get; set; } = new List<RefundDispute>();
}
