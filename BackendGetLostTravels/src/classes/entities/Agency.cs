namespace WebApplication2.classes.entities;

public partial class Agency
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? BannerUrl { get; set; }

    public string? LogoUrl { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public virtual ICollection<AgencyAnalytic> AgencyAnalytics { get; set; } = new List<AgencyAnalytic>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();

    public virtual User? User { get; set; }
}
