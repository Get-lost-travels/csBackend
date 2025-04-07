namespace WebApplication2.entities;

public partial class Message
{
    public int Id { get; set; }

    public int? SenderId { get; set; }

    public int? RecipientId { get; set; }

    public int? AgencyId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime? SentAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual Agency? Agency { get; set; }

    public virtual User? Recipient { get; set; }

    public virtual User? Sender { get; set; }
}
