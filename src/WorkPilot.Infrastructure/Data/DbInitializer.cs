using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Domain.Entities;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(WorkPilotDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // Schema migrations for pre-existing databases (AI Agents Audit trail and campaigns)
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[AIAgentActions]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [AIAgentActions] (
                        [Id] uniqueidentifier NOT NULL,
                        [BusinessId] uniqueidentifier NOT NULL,
                        [AgentType] int NOT NULL,
                        [ActionType] int NOT NULL,
                        [RiskLevel] int NOT NULL,
                        [Status] int NOT NULL,
                        [OwnerIntent] nvarchar(1000) NOT NULL,
                        [ReasoningSummary] nvarchar(max) NOT NULL,
                        [AgentChain] nvarchar(max) NOT NULL,
                        [EstimatedImpact] nvarchar(max) NOT NULL,
                        [EstimatedRevenue] decimal(18,2) NOT NULL,
                        [EstimatedBookings] int NOT NULL,
                        [ActualOutcome] nvarchar(max) NULL,
                        [ActualRevenue] decimal(18,2) NOT NULL,
                        [ActualBookings] int NOT NULL,
                        [TargetCustomerCount] int NOT NULL,
                        [OwnerNotes] nvarchar(max) NULL,
                        [FailureReason] nvarchar(max) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [ApprovedAt] datetime2 NULL,
                        [ExecutedAt] datetime2 NULL,
                        [CompletedAt] datetime2 NULL,
                        [CampaignId] uniqueidentifier NULL,
                        CONSTRAINT [PK_AIAgentActions] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_AIAgentActions_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE CASCADE
                    );
                END;");

            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Campaigns]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [Campaigns] (
                        [Id] uniqueidentifier NOT NULL,
                        [BusinessId] uniqueidentifier NOT NULL,
                        [AIAgentActionId] uniqueidentifier NULL,
                        [Name] nvarchar(max) NOT NULL,
                        [TargetSegment] nvarchar(max) NOT NULL,
                        [TargetCustomerCount] int NOT NULL,
                        [SubjectLine] nvarchar(max) NOT NULL,
                        [EmailBody] nvarchar(max) NOT NULL,
                        [OfferDescription] nvarchar(max) NOT NULL,
                        [Status] int NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [SentAt] datetime2 NULL,
                        [EmailsSent] int NOT NULL,
                        [EmailsFailed] int NOT NULL,
                        [BookingRequestsGenerated] int NOT NULL,
                        [BookingsConfirmed] int NOT NULL,
                        [RevenueGenerated] decimal(18,2) NOT NULL,
                        [CampaignCost] decimal(18,2) NOT NULL,
                        CONSTRAINT [PK_Campaigns] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Campaigns_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_Campaigns_AIAgentActions_AIAgentActionId] FOREIGN KEY ([AIAgentActionId]) REFERENCES [AIAgentActions] ([Id]) ON DELETE SET NULL
                    );
                END;");

            // Also make sure to add foreign key from AIAgentActions to Campaigns if it doesn't exist
            await context.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = N'FK_AIAgentActions_Campaigns_CampaignId')
                BEGIN
                    -- already exists
                    SELECT 1;
                END
                ELSE IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[AIAgentActions]') AND name = N'CampaignId')
                BEGIN
                    ALTER TABLE [AIAgentActions] ADD CONSTRAINT [FK_AIAgentActions_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE NO ACTION;
                END;");
        }
        catch { }

        // Schema migration helpers for pre-existing DBs
        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Bookings]') AND name = N'EmailDeliveryStatus') " +
                "BEGIN ALTER TABLE [Bookings] ADD [EmailDeliveryStatus] nvarchar(max) NULL; END; " +
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Bookings]') AND name = N'EmailDeliveryError') " +
                "BEGIN ALTER TABLE [Bookings] ADD [EmailDeliveryError] nvarchar(max) NULL; END;");
        }
        catch { }

        // Migrate Lead table for new columns
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Leads]') AND name = N'LastVisitDate')
                BEGIN ALTER TABLE [Leads] ADD [LastVisitDate] datetime2 NULL; END;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Leads]') AND name = N'TotalBookings')
                BEGIN ALTER TABLE [Leads] ADD [TotalBookings] int NOT NULL DEFAULT 0; END;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Leads]') AND name = N'TotalSpend')
                BEGIN ALTER TABLE [Leads] ADD [TotalSpend] decimal(18,2) NOT NULL DEFAULT 0; END;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Leads]') AND name = N'Tags')
                BEGIN ALTER TABLE [Leads] ADD [Tags] nvarchar(max) NULL; END;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Leads]') AND name = N'IsActive')
                BEGIN ALTER TABLE [Leads] ADD [IsActive] bit NOT NULL DEFAULT 1; END;");
        }
        catch { }

        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        Business business;
        if (!await context.Businesses.AnyAsync(b => b.Id == businessId))
        {
            business = new Business
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

            // Services
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

            // Availability Rules
            var rules = new[]
            {
                new AvailabilityRule { BusinessId = business.Id, DayOfWeek = DayOfWeek.Monday,    StartTime = new TimeSpan(18,0,0), EndTime = new TimeSpan(21,0,0), BufferMinutes = 15, IsActive = true },
                new AvailabilityRule { BusinessId = business.Id, DayOfWeek = DayOfWeek.Tuesday,   StartTime = new TimeSpan(18,0,0), EndTime = new TimeSpan(21,0,0), BufferMinutes = 15, IsActive = true },
                new AvailabilityRule { BusinessId = business.Id, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeSpan(18,0,0), EndTime = new TimeSpan(21,0,0), BufferMinutes = 15, IsActive = true },
                new AvailabilityRule { BusinessId = business.Id, DayOfWeek = DayOfWeek.Thursday,  StartTime = new TimeSpan(18,0,0), EndTime = new TimeSpan(21,0,0), BufferMinutes = 15, IsActive = true },
                new AvailabilityRule { BusinessId = business.Id, DayOfWeek = DayOfWeek.Friday,    StartTime = new TimeSpan(18,0,0), EndTime = new TimeSpan(21,0,0), BufferMinutes = 15, IsActive = true },
                new AvailabilityRule { BusinessId = business.Id, DayOfWeek = DayOfWeek.Saturday,  StartTime = new TimeSpan(8,0,0),  EndTime = new TimeSpan(12,0,0), BufferMinutes = 15, IsActive = true },
                new AvailabilityRule { BusinessId = business.Id, DayOfWeek = DayOfWeek.Sunday,    StartTime = new TimeSpan(9,0,0),  EndTime = new TimeSpan(14,0,0), BufferMinutes = 15, IsActive = true }
            };
            context.AvailabilityRules.AddRange(rules);
            await context.SaveChangesAsync();
        }
        else
        {
            business = await context.Businesses.FirstAsync(b => b.Id == businessId);
            // Add Sunday rule if missing (backward compat)
            if (!await context.AvailabilityRules.AnyAsync(r => r.BusinessId == businessId && r.DayOfWeek == DayOfWeek.Sunday))
            {
                context.AvailabilityRules.Add(new AvailabilityRule
                {
                    BusinessId = businessId,
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(14, 0, 0),
                    BufferMinutes = 15,
                    IsActive = true
                });
                await context.SaveChangesAsync();
            }
        }

        var ptServiceRef = await context.Services.FirstOrDefaultAsync(s => s.BusinessId == businessId && s.Name.Contains("Personal Training"));
        if (ptServiceRef == null)
        {
            ptServiceRef = new Service
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                BusinessId = businessId,
                Name = "Personal Training Session",
                Description = "60-minute tailored 1-on-1 personal workout session including strength and conditioning.",
                Price = 85.00m,
                DurationMinutes = 60,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Services.Add(ptServiceRef);
            await context.SaveChangesAsync();
        }

        var now = DateTime.UtcNow;
        var random = new Random(42);

        // Seed exactly the 50 customers requested
        var customerTemplates = new[]
        {
            // Active (last 5-20 days) — 20 customers
            ("Sarah Mitchell", "sarah.mitchell@email.com", 5, 425m, now.AddDays(-5),  "active"),
            ("James Carter",   "james.carter@email.com",   15, 1275m, now.AddDays(-7),  "active,vip"), // High Spend
            ("Emma Wilson",    "emma.wilson@email.com",    3, 255m, now.AddDays(-8), "active"),
            ("Liam Johnson",   "liam.johnson@email.com",   6, 510m, now.AddDays(-10), "active"),
            ("Olivia Brown",   "olivia.brown@email.com",   4, 340m, now.AddDays(-11), "active"),
            ("Noah Davis",     "noah.davis@email.com",     9, 765m, now.AddDays(-12), "active,premium"),
            ("Ava Martinez",   "ava.martinez@email.com",   2, 170m, now.AddDays(-13), "active"),
            ("William Thomas", "william.thomas@email.com", 7, 595m, now.AddDays(-14), "active"),
            ("Sophia Anderson","sophia.anderson@email.com",5, 425m, now.AddDays(-15), "active"),
            ("Mason Taylor",   "mason.taylor@email.com",   3, 255m, now.AddDays(-16), "active"),
            ("Isabella White", "isabella.white@email.com", 6, 510m, now.AddDays(-17), "active,premium"),
            ("Ethan Harris",   "ethan.harris@email.com",   4, 340m, now.AddDays(-18), "active"),
            ("Priya Sharma",   "priya.sharma@email.com",   20, 1700m, now.AddDays(-6), "active,vip,premium"), // High Spend
            ("Alex Johnson",   "alex.johnson@email.com",   5, 425m, now.AddDays(-9), "active"),
            ("Daniel Wilson",  "daniel.wilson@email.com",  14, 1190m, now.AddDays(-10), "active,vip"), // High Spend
            ("Ananya Rao",     "ananya.rao@email.com",     18, 1530m, now.AddDays(-11), "active,vip"), // High Spend
            ("Michael Brown",  "michael.brown@email.com",  6, 510m, now.AddDays(-12), "active"),
            ("David Miller",   "david.miller@email.com",   4, 340m, now.AddDays(-14), "active"),
            ("Emily Davis",    "emily.davis@email.com",    5, 425m, now.AddDays(-15), "active"),
            ("Chris Garcia",   "chris.garcia@email.com",   3, 255m, now.AddDays(-16), "active"),

            // Inactive 30–59 days — 8 customers
            ("Jennifer Harris", "jennifer.h@email.com",    4, 340m, now.AddDays(-32), "inactive,at-risk"),
            ("Susan Clark",     "susan.clark@email.com",    3, 255m, now.AddDays(-35), "inactive,at-risk"),
            ("Joseph Lewis",    "joseph.lewis@email.com",   5, 425m, now.AddDays(-38), "inactive,at-risk"),
            ("Thomas Robinson", "thomas.r@email.com",       2, 170m, now.AddDays(-42), "inactive,at-risk"),
            ("Charles Walker",  "charles.walker@email.com", 6, 510m, now.AddDays(-45), "inactive,at-risk"),
            ("Christopher Young","chris.young@email.com",   3, 255m, now.AddDays(-48), "inactive,at-risk"),
            ("Margaret Allen",  "margaret.a@email.com",     4, 340m, now.AddDays(-50), "inactive,at-risk"),
            ("Matthew King",    "matthew.king@email.com",   5, 425m, now.AddDays(-53), "inactive,at-risk"),

            // Inactive 60–89 days — 12 customers
            ("Lisa Wright",     "lisa.wright@email.com",    12, 1020m, now.AddDays(-62), "inactive-60,vip"), // High Spend
            ("Nancy Lopez",     "nancy.lopez@email.com",    3, 255m, now.AddDays(-64), "inactive-60"),
            ("Karen Hill",      "karen.hill@email.com",     4, 340m, now.AddDays(-66), "inactive-60"),
            ("Betty Scott",     "betty.scott@email.com",    6, 510m, now.AddDays(-68), "inactive-60"),
            ("Helen Green",     "helen.green@email.com",    10, 850m, now.AddDays(-70), "inactive-60,vip"), // High Spend
            ("Sandra Adams",    "sandra.adams@email.com",   8, 680m, now.AddDays(-72), "inactive-60"),
            ("Donna Baker",     "donna.baker@email.com",    3, 255m, now.AddDays(-75), "inactive-60"),
            ("Carol Nelson",    "carol.nelson@email.com",   5, 425m, now.AddDays(-77), "inactive-60"),
            ("Ruth Carter",     "ruth.carter@email.com",    4, 340m, now.AddDays(-79), "inactive-60"),
            ("Sharon Mitchell", "sharon.m@email.com",       6, 510m, now.AddDays(-81), "inactive-60"),
            ("Michelle Perez",  "michelle.perez@email.com", 3, 255m, now.AddDays(-83), "inactive-60"),
            ("Laura Roberts",   "laura.roberts@email.com",  7, 595m, now.AddDays(-85), "inactive-60"),

            // Inactive 90+ days — 10 customers
            ("Sarah Turner",    "sarah.turner@email.com",   3, 255m, now.AddDays(-95),  "inactive-90,lapsed"),
            ("Kimberly Phillips","kimberly.p@email.com",    5, 425m, now.AddDays(-100), "inactive-90,lapsed"),
            ("Deborah Campbell","deborah.c@email.com",      2, 170m, now.AddDays(-110), "inactive-90,lapsed"),
            ("Jessica Parker",  "jessica.parker@email.com", 4, 340m, now.AddDays(-120), "inactive-90,lapsed"),
            ("Shirley Evans",   "shirley.evans@email.com",  6, 510m, now.AddDays(-130), "inactive-90,lapsed"),
            ("Cynthia Edwards", "cynthia.e@email.com",      3, 255m, now.AddDays(-140), "inactive-90,lapsed"),
            ("Angela Collins",  "angela.collins@email.com", 7, 595m, now.AddDays(-150), "inactive-90,lapsed"),
            ("Melissa Stewart", "melissa.s@email.com",      2, 170m, now.AddDays(-160), "inactive-90,lapsed"),
            ("Brenda Morris",   "brenda.morris@email.com",  4, 340m, now.AddDays(-170), "inactive-90,lapsed"),
            ("Amy Rogers",      "amy.rogers@email.com",     5, 425m, now.AddDays(-180), "inactive-90,lapsed"),
        };

        // Idempotent upsert/repair loop based on unique Email
        var processedLeads = new List<Lead>();
        foreach (var (name, email, bookings, spend, lastVisit, tags) in customerTemplates)
        {
            var lead = await context.Leads
                .FirstOrDefaultAsync(l => l.BusinessId == businessId && l.Email == email);

            if (lead == null)
            {
                lead = new Lead
                {
                    BusinessId = businessId,
                    Name = name,
                    Email = email,
                    Status = "Converted",
                    Source = "WebChat",
                    LastVisitDate = lastVisit,
                    TotalBookings = bookings,
                    TotalSpend = spend,
                    Tags = tags,
                    IsActive = tags.Contains("active"),
                    CreatedAt = lastVisit.AddDays(-random.Next(30, 180))
                };
                context.Leads.Add(lead);
            }
            else
            {
                // Repair/update properties of existing record relative to now
                lead.Name = name;
                lead.LastVisitDate = lastVisit;
                lead.TotalBookings = bookings;
                lead.TotalSpend = spend;
                lead.Tags = tags;
                lead.IsActive = tags.Contains("active");
                lead.Status = "Converted";
                lead.Source = "WebChat";
            }
            processedLeads.Add(lead);
        }
        await context.SaveChangesAsync();

        // Safe cleanup of existing demo bookings to ensure dates align perfectly
        var demoBookings = await context.Bookings
            .Include(b => b.BookingRequest)
            .Where(b => b.BookingRequest != null && b.BookingRequest.BusinessId == businessId && b.GoogleCalendarEventId != null && b.GoogleCalendarEventId.StartsWith("demo_event_"))
            .ToListAsync();

        if (demoBookings.Any())
        {
            context.Bookings.RemoveRange(demoBookings);
            context.BookingRequests.RemoveRange(demoBookings.Select(b => b.BookingRequest).OfType<BookingRequest>());
            await context.SaveChangesAsync();
        }

        // Recreate fresh historical bookings connected to active leads
        var activeLeads = processedLeads.Where(l => l.Tags != null && l.Tags.Contains("active")).ToArray();
        for (int i = 0; i < 18; i++)
        {
            var daysAgo = random.Next(1, 60);
            var startTime = now.AddDays(-daysAgo).Date.AddHours(18 + random.Next(0, 2));
            var targetLead = activeLeads[i % activeLeads.Length];

            var bookingReq = new BookingRequest
            {
                BusinessId = businessId,
                LeadId = targetLead.Id,
                ServiceId = ptServiceRef.Id,
                RequestedStartTime = startTime,
                RequestedEndTime = startTime.AddMinutes(60),
                ProposedSlotSummary = $"Personal Training Session on {startTime:ddd, MMM d} @ {startTime:h:mm tt}",
                Status = BookingRequestStatus.Approved,
                CreatedAt = startTime.AddDays(-1),
                UpdatedAt = startTime
            };
            context.BookingRequests.Add(bookingReq);

            var booking = new Booking
            {
                BookingRequestId = bookingReq.Id,
                GoogleCalendarEventId = $"demo_event_{i}",
                Status = BookingStatus.Confirmed,
                ConfirmedAt = startTime,
                EmailDeliveryStatus = "Simulated",
                ConfirmationEmailSent = true
            };
            context.Bookings.Add(booking);
        }

        await context.SaveChangesAsync();
    }
}
