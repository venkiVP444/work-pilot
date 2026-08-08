using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkPilot.Application.Common.Interfaces;

public enum EmailDeliveryStatus
{
    Sent,
    Failed,
    Simulated
}

public record EmailResult(
    bool Success,
    EmailDeliveryStatus Status,
    string? ErrorMessage = null
);

public interface IEmailService
{
    Task<EmailResult> SendBookingConfirmationEmailAsync(
        string recipientEmail,
        string recipientName,
        string businessName,
        string serviceName,
        DateTime appointmentStartTime,
        DateTime appointmentEndTime,
        string location,
        string cancellationPolicy,
        CancellationToken cancellationToken = default);

    Task<EmailResult> SendCampaignEmailAsync(
        string recipientEmail,
        string recipientName,
        string businessName,
        string subjectLine,
        string htmlBody,
        CancellationToken cancellationToken = default);
}

