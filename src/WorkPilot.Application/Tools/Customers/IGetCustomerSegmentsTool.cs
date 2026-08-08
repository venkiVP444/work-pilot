using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WorkPilot.Application.Tools.Customers;

public record CustomerSegmentSummary(
    string Name,
    int Count,
    decimal TotalRevenueContribution
);

public interface IGetCustomerSegmentsTool
{
    Task<List<CustomerSegmentSummary>> ExecuteAsync(Guid businessId, CancellationToken cancellationToken = default);
}
