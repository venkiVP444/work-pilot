using Microsoft.EntityFrameworkCore;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Domain.Entities;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Infrastructure.Data;

public class WorkPilotDbContext : DbContext, IWorkPilotDbContext
{
    public WorkPilotDbContext(DbContextOptions<WorkPilotDbContext> options) : base(options)
    {
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<AvailabilityRule> AvailabilityRules => Set<AvailabilityRule>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<AIInteractionLog> AIInteractionLogs => Set<AIInteractionLog>();
    public DbSet<AIAgentAction> AIAgentActions => Set<AIAgentAction>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TimeZone).HasMaxLength(100);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Business)
                  .WithMany(b => b.Services)
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AvailabilityRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Business)
                  .WithMany(b => b.AvailabilityRules)
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TotalSpend).HasColumnType("decimal(18,2)");
            entity.Ignore(e => e.DaysSinceLastVisit);
            entity.HasOne(e => e.Business)
                  .WithMany(b => b.Leads)
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Lead)
                  .WithMany(l => l.Conversations)
                  .HasForeignKey(e => e.LeadId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Lead)
                  .WithMany(l => l.BookingRequests)
                  .HasForeignKey(e => e.LeadId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Service)
                  .WithMany()
                  .HasForeignKey(e => e.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.BookingRequest)
                  .WithOne(br => br.Booking)
                  .HasForeignKey<Booking>(e => e.BookingRequestId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIInteractionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.HasOne(e => e.Business)
                  .WithMany()
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIAgentAction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EstimatedRevenue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualRevenue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OwnerIntent).HasMaxLength(1000);
            entity.HasOne(e => e.Business)
                  .WithMany()
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Campaign)
                  .WithOne(c => c.AIAgentAction)
                  .HasForeignKey<Campaign>(c => c.AIAgentActionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RevenueGenerated).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CampaignCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Name).HasMaxLength(300);
            entity.HasOne(e => e.Business)
                  .WithMany()
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
