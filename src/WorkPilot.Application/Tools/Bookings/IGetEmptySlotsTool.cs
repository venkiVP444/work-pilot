using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkPilot.Application.Tools.Bookings;

public interface IGetEmptySlotsTool
{
    Task<int> ExecuteAsync(Guid businessId, CancellationToken cancellationToken = default);
}
