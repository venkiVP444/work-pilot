using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(WorkPilotDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Bookings]') AND name = N'EmailDeliveryStatus') " +
                "BEGIN ALTER TABLE [Bookings] ADD [EmailDeliveryStatus] nvarchar(max) NULL; END; " +
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Bookings]') AND name = N'EmailDeliveryError') " +
                "BEGIN ALTER TABLE [Bookings] ADD [EmailDeliveryError] nvarchar(max) NULL; END;");
        }
        catch { }

        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        if (!await context.Businesses.AnyAsync(b => b.Id == businessId))
        {
            // Create Default Demo Business: FitPro Personal Training
            var business = new Business
            {
                Id = businessId,
                Name = "FitPro Personal Training",
                Description = "Premium 1-on-1 personal fitness coaching and studio training.",
                Location = "123 Fitness Way, Suite 100, New York, NY 10001",
                ContactEmail = "trainer@fitproexample.com",
                TimeZone = "Eastern Standard Time",
                CancellationPolicy = "24-hour cancellation notice required for full refund.",
                CommunicationTone = "Energetic, motivating, friendly, and professional",
                IsCalendarConnected = true,
                GoogleCalendarId = "primary",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Businesses.Add(business);

            // Add Demo Services
            var ptService = new Service
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                BusinessId = business.Id,
                Name = "Personal Training Session",
                Description = "60-minute tailored 1-on-1 personal workout session including strength and conditioning.",
                Price = 85.00m,
                DurationMinutes = 60,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var assessmentService = new Service
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                BusinessId = business.Id,
                Name = "Fitness & Nutrition Assessment",
                Description = "45-minute comprehensive body composition and goal planning consultation.",
                Price = 50.00m,
                DurationMinutes = 45,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Services.AddRange(ptService, assessmentService);

            // Add Demo Availability Rules for all days (Monday - Sunday)
            var rules = new[]
            {
                new AvailabilityRule
                {
                    BusinessId = business.Id,
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeSpan(18, 0, 0),
                    EndTime = new TimeSpan(21, 0, 0),
                    BufferMinutes = 15,
                    IsActive = true
                },
                new AvailabilityRule
                {
                    BusinessId = business.Id,
                    DayOfWeek = DayOfWeek.Tuesday,
                    StartTime = new TimeSpan(18, 0, 0),
                    EndTime = new TimeSpan(21, 0, 0),
                    BufferMinutes = 15,
                    IsActive = true
                },
                new AvailabilityRule
                {
                    BusinessId = business.Id,
                    DayOfWeek = DayOfWeek.Wednesday,
                    StartTime = new TimeSpan(18, 0, 0),
                    EndTime = new TimeSpan(21, 0, 0),
                    BufferMinutes = 15,
                    IsActive = true
                },
                new AvailabilityRule
                {
                    BusinessId = business.Id,
                    DayOfWeek = DayOfWeek.Thursday,
                    StartTime = new TimeSpan(18, 0, 0),
                    EndTime = new TimeSpan(21, 0, 0),
                    BufferMinutes = 15,
                    IsActive = true
                },
                new AvailabilityRule
                {
                    BusinessId = business.Id,
                    DayOfWeek = DayOfWeek.Friday,
                    StartTime = new TimeSpan(18, 0, 0),
                    EndTime = new TimeSpan(21, 0, 0),
                    BufferMinutes = 15,
                    IsActive = true
                },
                new AvailabilityRule
                {
                    BusinessId = business.Id,
                    DayOfWeek = DayOfWeek.Saturday,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    BufferMinutes = 15,
                    IsActive = true
                },
                new AvailabilityRule
                {
                    BusinessId = business.Id,
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(14, 0, 0),
                    BufferMinutes = 15,
                    IsActive = true
                }
            };

            context.AvailabilityRules.AddRange(rules);
            await context.SaveChangesAsync();
        }
        else
        {
            // Ensure Sunday rule exists if DB was already created previously
            if (!await context.AvailabilityRules.AnyAsync(r => r.BusinessId == businessId && r.DayOfWeek == DayOfWeek.Sunday))
            {
                var sundayRule = new AvailabilityRule
                {
                    BusinessId = businessId,
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(14, 0, 0),
                    BufferMinutes = 15,
                    IsActive = true
                };
                context.AvailabilityRules.Add(sundayRule);
                await context.SaveChangesAsync();
            }
        }
    }
}
