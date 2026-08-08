using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkPilot.Application.Tools.Communications;

public record SendEmailInput(
    string RecipientEmail,
    string RecipientName,
    string BusinessName,
    string SubjectLine,
    string HtmlBody
);

public interface ISendCampaignEmailTool
{
    Task<bool> ExecuteAsync(SendEmailInput input, CancellationToken cancellationToken = default);
}
