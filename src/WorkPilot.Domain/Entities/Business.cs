using System;
using System.Collections.Generic;

namespace WorkPilot.Domain.Entities;

public class Business
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "Eastern Standard Time";
    public string CancellationPolicy { get; set; } = string.Empty;
    public string CommunicationTone { get; set; } = "Friendly and professional";
    
    // Google Calendar Integration
    public string? GoogleCalendarId { get; set; }
    public string? GoogleRefreshToken { get; set; }
    public bool IsCalendarConnected { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<AvailabilityRule> AvailabilityRules { get; set; } = new List<AvailabilityRule>();
    public ICollection<Lead> Leads { get; set; } = new List<Lead>();
}
