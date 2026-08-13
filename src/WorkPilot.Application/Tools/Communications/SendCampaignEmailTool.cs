using System;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.Common.Interfaces;

namespace WorkPilot.Application.Tools.Communications;

public class SendCampaignEmailTool : ISendCampaignEmailTool
{
    private readonly IEmailService _emailService;

    public SendCampaignEmailTool(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<bool> ExecuteAsync(SendEmailInput input, CancellationToken cancellationToken = default)
    {
        var result = await _emailService.SendCampaignEmailAsync(
            input.RecipientEmail,
            input.RecipientName,
            input.BusinessName,
            input.SubjectLine,
            input.HtmlBody,
            cancellationToken
        );

        return result.Success;
    }
}
