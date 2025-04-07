using Microsoft.EntityFrameworkCore;

namespace WebApplication2.classes.entities;

public partial class DatabaseContext : DbContext
{
    public DatabaseContext()
    {
    }

    public DatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Agency> Agencies { get; set; }

    public virtual DbSet<AgencyAnalytic> AgencyAnalytics { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<ETicket> ETickets { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<RefundDispute> RefundDisputes { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceAvailability> ServiceAvailabilities { get; set; }

    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }

    public virtual DbSet<ServiceMedium> ServiceMedia { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserPreference> UserPreferences { get; set; }

    public virtual DbSet<UserSavedSearch> UserSavedSearches { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=database.sqlite");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agency>(entity =>
        {
            entity.ToTable("agencies");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.BannerUrl).HasColumnName("banner_url");
            entity.Property(e => e.LogoUrl).HasColumnName("logo_url");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Agencies).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AgencyAnalytic>(entity =>
        {
            entity.ToTable("agency_analytics");

            entity.HasIndex(e => new { e.AgencyId, e.Date }, "IX_agency_analytics_agency_id_date").IsUnique();

            entity.HasIndex(e => new { e.AgencyId, e.Date }, "idx_agency_analytics_agency_id_date");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AgencyId).HasColumnName("agency_id");
            entity.Property(e => e.Bookings)
                .HasDefaultValue(0)
                .HasColumnName("bookings");
            entity.Property(e => e.Date)
                .HasColumnType("DATE")
                .HasColumnName("date");
            entity.Property(e => e.Revenue)
                .HasDefaultValue(0.0)
                .HasColumnName("revenue");
            entity.Property(e => e.Views)
                .HasDefaultValue(0)
                .HasColumnName("views");

            entity.HasOne(d => d.Agency).WithMany(p => p.AgencyAnalytics).HasForeignKey(d => d.AgencyId);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");

            entity.HasIndex(e => e.ServiceId, "idx_bookings_service_id");

            entity.HasIndex(e => e.UserId, "idx_bookings_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BookingDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("booking_date");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Service).WithMany(p => p.Bookings).HasForeignKey(d => d.ServiceId);

            entity.HasOne(d => d.User).WithMany(p => p.Bookings).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<ETicket>(entity =>
        {
            entity.ToTable("e_tickets");

            entity.HasIndex(e => e.BookingId, "IX_e_tickets_booking_id").IsUnique();

            entity.HasIndex(e => e.TicketCode, "IX_e_tickets_ticket_code").IsUnique();

            entity.HasIndex(e => e.BookingId, "idx_e_tickets_booking_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.IssuedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("issued_at");
            entity.Property(e => e.QrCodeUrl).HasColumnName("qr_code_url");
            entity.Property(e => e.TicketCode).HasColumnName("ticket_code");

            entity.HasOne(d => d.Booking).WithOne(p => p.ETicket).HasForeignKey<ETicket>(d => d.BookingId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");

            entity.HasIndex(e => e.AgencyId, "idx_messages_agency_id");

            entity.HasIndex(e => e.RecipientId, "idx_messages_recipient_id");

            entity.HasIndex(e => e.SenderId, "idx_messages_sender_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AgencyId).HasColumnName("agency_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.RecipientId).HasColumnName("recipient_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("sent_at");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.Agency).WithMany(p => p.Messages).HasForeignKey(d => d.AgencyId);

            entity.HasOne(d => d.Recipient).WithMany(p => p.MessageRecipients).HasForeignKey(d => d.RecipientId);

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders).HasForeignKey(d => d.SenderId);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");

            entity.HasIndex(e => e.UserId, "idx_notifications_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnType("BOOLEAN")
                .HasColumnName("is_read");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.HasIndex(e => e.BookingId, "idx_payments_booking_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("payment_date");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments).HasForeignKey(d => d.BookingId);
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("payment_methods");

            entity.HasIndex(e => e.UserId, "idx_payment_methods_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.IsDefault)
                .HasDefaultValue(false)
                .HasColumnType("BOOLEAN")
                .HasColumnName("is_default");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentMethods).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<RefundDispute>(entity =>
        {
            entity.ToTable("refund_disputes");

            entity.HasIndex(e => e.BookingId, "idx_refund_disputes_booking_id");

            entity.HasIndex(e => e.Status, "idx_refund_disputes_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdminVerdict).HasColumnName("admin_verdict");
            entity.Property(e => e.AgencyResponse).HasColumnName("agency_response");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CustomerExplanation).HasColumnName("customer_explanation");
            entity.Property(e => e.OpenedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("opened_at");
            entity.Property(e => e.OpenedBy).HasColumnName("opened_by");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.ResolvedAt)
                .HasColumnType("TIMESTAMP")
                .HasColumnName("resolved_at");
            entity.Property(e => e.ResolvedBy).HasColumnName("resolved_by");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.Booking).WithMany(p => p.RefundDisputes).HasForeignKey(d => d.BookingId);

            entity.HasOne(d => d.OpenedByNavigation).WithMany(p => p.RefundDisputeOpenedByNavigations).HasForeignKey(d => d.OpenedBy);

            entity.HasOne(d => d.Payment).WithMany(p => p.RefundDisputes).HasForeignKey(d => d.PaymentId);

            entity.HasOne(d => d.ResolvedByNavigation).WithMany(p => p.RefundDisputeResolvedByNavigations).HasForeignKey(d => d.ResolvedBy);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews");

            entity.HasIndex(e => e.CreatedAt, "idx_reviews_created_at");

            entity.HasIndex(e => e.Rating, "idx_reviews_rating");

            entity.HasIndex(e => e.ServiceId, "idx_reviews_service_id");

            entity.HasIndex(e => e.UserId, "idx_reviews_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VerifiedPurchase)
                .HasDefaultValue(false)
                .HasColumnType("BOOLEAN")
                .HasColumnName("verified_purchase");

            entity.HasOne(d => d.Service).WithMany(p => p.Reviews).HasForeignKey(d => d.ServiceId);

            entity.HasOne(d => d.User).WithMany(p => p.Reviews).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("services");

            entity.HasIndex(e => e.Location, "idx_services_location");

            entity.HasIndex(e => e.Title, "idx_services_title");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AgencyId).HasColumnName("agency_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.Exclusions).HasColumnName("exclusions");
            entity.Property(e => e.Inclusions).HasColumnName("inclusions");
            entity.Property(e => e.Itinerary).HasColumnName("itinerary");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Terms).HasColumnName("terms");
            entity.Property(e => e.Title).HasColumnName("title");

            entity.HasOne(d => d.Agency).WithMany(p => p.Services).HasForeignKey(d => d.AgencyId);

            entity.HasOne(d => d.Category).WithMany(p => p.Services).HasForeignKey(d => d.CategoryId);
        });

        modelBuilder.Entity<ServiceAvailability>(entity =>
        {
            entity.ToTable("service_availability");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "idx_service_availability_dates");

            entity.HasIndex(e => e.ServiceId, "idx_service_availability_service_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Capacity)
                .HasDefaultValue(1)
                .HasColumnName("capacity");
            entity.Property(e => e.EndDate)
                .HasColumnType("DATE")
                .HasColumnName("end_date");
            entity.Property(e => e.RemainingSpots).HasColumnName("remaining_spots");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.StartDate)
                .HasColumnType("DATE")
                .HasColumnName("start_date");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceAvailabilities).HasForeignKey(d => d.ServiceId);
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.ToTable("service_categories");

            entity.HasIndex(e => e.Name, "IX_service_categories_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<ServiceMedium>(entity =>
        {
            entity.ToTable("service_media");

            entity.HasIndex(e => e.ServiceId, "idx_service_media_service_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Caption).HasColumnName("caption");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.IsFeatured)
                .HasDefaultValue(false)
                .HasColumnType("BOOLEAN")
                .HasColumnName("is_featured");
            entity.Property(e => e.MediaType).HasColumnName("media_type");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.Url).HasColumnName("url");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceMedia).HasForeignKey(d => d.ServiceId);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.Key);

            entity.ToTable("system_settings");

            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.SystemSettings).HasForeignKey(d => d.UpdatedBy);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "IX_users_email").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuthProvider).HasColumnName("auth_provider");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Username).HasColumnName("username");
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("user_preferences");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.NotificationPreferences)
                .HasDefaultValue("{}")
                .HasColumnName("notification_preferences");
            entity.Property(e => e.ThemePreference)
                .HasDefaultValue("system")
                .HasColumnName("theme_preference");

            entity.HasOne(d => d.User).WithOne(p => p.UserPreference)
                .HasForeignKey<UserPreference>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UserSavedSearch>(entity =>
        {
            entity.HasKey(e => e.Request);

            entity.ToTable("user_saved_searches");

            entity.Property(e => e.Request).HasColumnName("request");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Filters).HasColumnName("filters");
            entity.Property(e => e.Query).HasColumnName("query");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserSavedSearches).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");

            entity.HasIndex(e => e.Token, "IX_user_sessions_token").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions).HasForeignKey(d => d.UserId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
