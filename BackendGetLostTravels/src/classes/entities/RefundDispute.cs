namespace WebApplication2.classes.entities;

public partial class RefundDispute
{
    public int Id { get; set; }

    public int? BookingId { get; set; }

    public int? PaymentId { get; set; }

    public int? OpenedBy { get; set; }

    public string Status { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public string? CustomerExplanation { get; set; }

    public string? AgencyResponse { get; set; }

    public string? AdminVerdict { get; set; }

    public DateTime? OpenedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public int? ResolvedBy { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual User? OpenedByNavigation { get; set; }

    public virtual Payment? Payment { get; set; }

    public virtual User? ResolvedByNavigation { get; set; }
}
