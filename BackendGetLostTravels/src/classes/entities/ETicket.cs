namespace WebApplication2.classes.entities;

public partial class ETicket
{
    public int Id { get; set; }

    public int? BookingId { get; set; }

    public string TicketCode { get; set; } = null!;

    public string? QrCodeUrl { get; set; }

    public DateTime? IssuedAt { get; set; }

    public virtual Booking? Booking { get; set; }
}
