namespace WebApplication2.classes.entities;

public partial class ServiceMedium
{
    public int Id { get; set; }

    public int? ServiceId { get; set; }

    public string MediaType { get; set; } = null!;

    public string Url { get; set; } = null!;

    public bool? IsFeatured { get; set; }

    public string? Caption { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Service? Service { get; set; }
}
