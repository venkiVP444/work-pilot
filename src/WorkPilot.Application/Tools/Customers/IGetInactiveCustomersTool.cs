using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Application.Tools.Customers;

public interface IGetInactiveCustomersTool
{
    Task<List<Lead>> ExecuteAsync(Guid businessId, int thresholdDays, CancellationToken cancellationToken = default);
}
