using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Application.Agents;

public record CustomerGrowthInput(
    Guid BusinessId,
    string TargetSegment
);

public record CustomerGrowthOutput(
    string SegmentDescription,
    List<Lead> TargetCustomers,
    int TotalCount
);

public interface ICustomerGrowthAgent
{
    Task<CustomerGrowthOutput> IdentifyReactivationCandidatesAsync(CustomerGrowthInput input, CancellationToken cancellationToken = default);
}
