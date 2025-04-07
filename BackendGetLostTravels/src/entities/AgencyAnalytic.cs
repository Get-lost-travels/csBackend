namespace WebApplication2.entities;

public partial class AgencyAnalytic
{
    public int Id { get; set; }

    public int? AgencyId { get; set; }

    public DateTime Date { get; set; }

    public int? Views { get; set; }

    public int? Bookings { get; set; }

    public double? Revenue { get; set; }

    public virtual Agency? Agency { get; set; }
}
