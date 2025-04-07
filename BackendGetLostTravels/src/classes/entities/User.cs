namespace WebApplication2.classes.entities;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string Role { get; set; } = null!;

    public string AuthProvider { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Agency> Agencies { get; set; } = new List<Agency>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Message> MessageRecipients { get; set; } = new List<Message>();

    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();

    public virtual ICollection<RefundDispute> RefundDisputeOpenedByNavigations { get; set; } = new List<RefundDispute>();

    public virtual ICollection<RefundDispute> RefundDisputeResolvedByNavigations { get; set; } = new List<RefundDispute>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();

    public virtual UserPreference? UserPreference { get; set; }

    public virtual ICollection<UserSavedSearch> UserSavedSearches { get; set; } = new List<UserSavedSearch>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
