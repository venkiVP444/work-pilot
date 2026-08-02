using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Application.Common.Interfaces;

public interface IWorkPilotDbContext
{
    DbSet<Business> Businesses { get; }
    DbSet<Service> Services { get; }
    DbSet<AvailabilityRule> AvailabilityRules { get; }
    DbSet<Lead> Leads { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<ConversationMessage> ConversationMessages { get; }
    DbSet<BookingRequest> BookingRequests { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<AIInteractionLog> AIInteractionLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
