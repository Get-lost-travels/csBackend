namespace WebApplication2.classes.requests.services;

public class CreateServiceRequest
{
    public int AgencyId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = null!;
    public double Price { get; set; }
    public string Location { get; set; } = null!;
    public int? Duration { get; set; }
    public string? Description { get; set; }
    public string? Itinerary { get; set; }
    public string? Inclusions { get; set; }
    public string? Exclusions { get; set; }
    public string? Terms { get; set; }
}
