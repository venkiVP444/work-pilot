using System;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;

namespace WorkPilot.Application.Tools.Analytics;

public interface IGetBusinessSnapshotTool
{
    Task<BusinessSnapshotDto> ExecuteAsync(Guid businessId, CancellationToken cancellationToken = default);
}
