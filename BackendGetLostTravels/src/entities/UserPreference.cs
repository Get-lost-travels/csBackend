namespace WebApplication2.entities;

public partial class UserPreference
{
    public int UserId { get; set; }

    public string ThemePreference { get; set; } = null!;

    public string? NotificationPreferences { get; set; }

    public virtual User User { get; set; } = null!;
}
