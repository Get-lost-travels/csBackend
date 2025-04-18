﻿namespace WebApplication2.classes.entities;

public partial class Service
{
    public int Id { get; set; }

    public int? AgencyId { get; set; }

    public int? CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public double Price { get; set; }

    public string Location { get; set; } = null!;

    public int? Duration { get; set; }

    public string? Description { get; set; }

    public string? Itinerary { get; set; }

    public string? Inclusions { get; set; }

    public string? Exclusions { get; set; }

    public string? Terms { get; set; }

    public virtual Agency? Agency { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ServiceCategory? Category { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<ServiceAvailability> ServiceAvailabilities { get; set; } = new List<ServiceAvailability>();

    public virtual ICollection<ServiceMedium> ServiceMedia { get; set; } = new List<ServiceMedium>();
}
