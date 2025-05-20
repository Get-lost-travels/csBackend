namespace WebApplication2.classes.requests.agency;

public class AgencyRequest
{
    public int? UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? BannerUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
}