namespace WebApplication2.entities;

public partial class ServiceAvailability
{
    public int Id { get; set; }

    public int? ServiceId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Capacity { get; set; }

    public int RemainingSpots { get; set; }

    public virtual Service? Service { get; set; }
}
