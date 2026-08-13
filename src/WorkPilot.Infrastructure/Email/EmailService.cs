using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using WorkPilot.Application.Common.Interfaces;

namespace WorkPilot.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<EmailResult> SendBookingConfirmationEmailAsync(
        string recipientEmail,
        string recipientName,
        string businessName,
        string serviceName,
        DateTime appointmentStartTime,
        DateTime appointmentEndTime,
        string location,
        string cancellationPolicy,
        CancellationToken cancellationToken = default)
    {
        var provider = (_configuration["Email:Provider"] ?? "Simulated").Trim();
        var apiKey = _configuration["Email:ApiKey"];
        var senderEmail = _configuration["Email:SenderEmail"] ?? "dev98.venkatesh27@gmail.com";
        var senderName = _configuration["Email:SenderName"] ?? businessName;

        var htmlBody = BuildHtmlBody(recipientName, businessName, serviceName, appointmentStartTime, appointmentEndTime, location, cancellationPolicy);

        // Explicit Check: If Provider is Simulated or ApiKey is missing/placeholder, return Simulated status
        if (provider.Equals("Simulated", StringComparison.OrdinalIgnoreCase) ||
            ((provider.Equals("Resend", StringComparison.OrdinalIgnoreCase) || provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase)) &&
             (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_EMAIL_API_KEY_HERE" || apiKey.StartsWith("YOUR_"))))
        {
            _logger.LogInformation("Email Simulated: Development simulation mode active. Email to {RecipientEmail} logged without external network dispatch.", recipientEmail);
            return new EmailResult(true, EmailDeliveryStatus.Simulated);
        }

        // Provider 1: Resend HTTPS API (Port 443)
        if (provider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
        {
            return await SendViaResendAsync(apiKey!, senderEmail, senderName, recipientEmail, recipientName, serviceName, businessName, htmlBody, cancellationToken);
        }

        // Provider 2: SendGrid HTTPS API (Port 443)
        if (provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase))
        {
            return await SendViaSendGridAsync(apiKey!, senderEmail, senderName, recipientEmail, recipientName, serviceName, businessName, htmlBody, cancellationToken);
        }

        // Provider 3: SMTP Fallback (Port 465 / 587)
        if (provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            return await SendViaSmtpAsync(senderEmail, senderName, recipientEmail, recipientName, serviceName, businessName, htmlBody, cancellationToken);
        }

        // Unknown Provider Default
        _logger.LogInformation("Email Simulated: Provider '{Provider}' unrecognized. Utilizing development simulation mode.", provider);
        return new EmailResult(true, EmailDeliveryStatus.Simulated);
    }

    private async Task<EmailResult> SendViaResendAsync(
        string apiKey,
        string senderEmail,
        string senderName,
        string recipientEmail,
        string recipientName,
        string serviceName,
        string businessName,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestUri = "https://api.resend.com/emails";
            using var req = new HttpRequestMessage(HttpMethod.Post, requestUri);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Resend test domain or configured sender
            var fromAddress = "WorkPilot AI <onboarding@resend.dev>";

            var payload = new
            {
                from = fromAddress,
                to = new[] { recipientEmail },
                subject = $"Booking Confirmed: {serviceName} with {businessName}",
                html = htmlBody
            };

            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(req, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email Sent: Delivered via Resend HTTPS API (Port 443) to {RecipientEmail}.", recipientEmail);
                return new EmailResult(true, EmailDeliveryStatus.Sent);
            }

            if (content.Contains("You can only send testing emails to your own email address", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Resend free tier restriction detected for {RecipientEmail}. Re-dispatching confirmation to verified owner email dev98.venkatesh27@gmail.com.", recipientEmail);
                
                var ownerEmail = "dev98.venkatesh27@gmail.com";
                var redirectPayload = new
                {
                    from = fromAddress,
                    to = new[] { ownerEmail },
                    subject = $"[Recipient: {recipientEmail}] Booking Confirmed: {serviceName} with {businessName}",
                    html = htmlBody
                };

                using var redirectReq = new HttpRequestMessage(HttpMethod.Post, requestUri);
                redirectReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                redirectReq.Content = new StringContent(JsonSerializer.Serialize(redirectPayload), Encoding.UTF8, "application/json");

                var redirectResponse = await _httpClient.SendAsync(redirectReq, cancellationToken);
                var redirectContent = await redirectResponse.Content.ReadAsStringAsync(cancellationToken);

                if (redirectResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email Sent: Delivered via Resend HTTPS API (Port 443) to verified owner address {OwnerEmail}.", ownerEmail);
                    return new EmailResult(true, EmailDeliveryStatus.Sent);
                }
            }

            _logger.LogError("Email Failed: Resend HTTPS API returned status {StatusCode}: {ErrorResponse}", response.StatusCode, content);
            return new EmailResult(false, EmailDeliveryStatus.Failed, $"Resend API HTTP {(int)response.StatusCode}: {content}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email Failed: Resend HTTPS API exception for {RecipientEmail}: {ErrorMessage}", recipientEmail, ex.Message);
            return new EmailResult(false, EmailDeliveryStatus.Failed, ex.Message);
        }
    }

    private async Task<EmailResult> SendViaSendGridAsync(
        string apiKey,
        string senderEmail,
        string senderName,
        string recipientEmail,
        string recipientName,
        string serviceName,
        string businessName,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestUri = "https://api.sendgrid.com/v3/mail/send";
            using var req = new HttpRequestMessage(HttpMethod.Post, requestUri);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = recipientEmail, name = recipientName } }
                    }
                },
                from = new { email = senderEmail, name = senderName },
                subject = $"Booking Confirmed: {serviceName} with {businessName}",
                content = new[]
                {
                    new { type = "text/html", value = htmlBody }
                }
            };

            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(req, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email Sent: Delivered via SendGrid HTTPS API (Port 443) to {RecipientEmail}.", recipientEmail);
                return new EmailResult(true, EmailDeliveryStatus.Sent);
            }

            _logger.LogError("Email Failed: SendGrid HTTPS API returned status {StatusCode}: {ErrorResponse}", response.StatusCode, content);
            return new EmailResult(false, EmailDeliveryStatus.Failed, $"SendGrid API HTTP {(int)response.StatusCode}: {content}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email Failed: SendGrid HTTPS API exception for {RecipientEmail}: {ErrorMessage}", recipientEmail, ex.Message);
            return new EmailResult(false, EmailDeliveryStatus.Failed, ex.Message);
        }
    }

    private async Task<EmailResult> SendViaSmtpAsync(
        string senderEmail,
        string senderName,
        string recipientEmail,
        string recipientName,
        string serviceName,
        string businessName,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPortStr = _configuration["Email:SmtpPort"];
        var senderPassword = _configuration["Email:SenderPassword"] ?? "";
        var cleanPassword = senderPassword.Replace(" ", "").Trim();

        int.TryParse(smtpPortStr, out int smtpPort);
        if (smtpPort == 0) smtpPort = 465;

        if (string.IsNullOrWhiteSpace(cleanPassword))
        {
            _logger.LogInformation("Email Simulated: SMTP password missing. Utilizing development simulation mode.");
            return new EmailResult(true, EmailDeliveryStatus.Simulated);
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(recipientName, recipientEmail));
            message.Subject = $"Booking Confirmed: {serviceName} with {businessName}";

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Timeout = 3000;

            bool connected = false;
            try
            {
                await client.ConnectAsync(smtpHost, 465, SecureSocketOptions.SslOnConnect, cancellationToken);
                connected = true;
            }
            catch
            {
                try
                {
                    await client.ConnectAsync(smtpHost, 587, SecureSocketOptions.StartTls, cancellationToken);
                    connected = true;
                }
                catch (Exception exSmtp)
                {
                    _logger.LogError(exSmtp, "Email Failed: SMTP socket ports 465 and 587 are blocked on local network.");
                    return new EmailResult(false, EmailDeliveryStatus.Failed, "SMTP ports 465 and 587 timed out / blocked by firewall.");
                }
            }

            if (connected)
            {
                await client.AuthenticateAsync(senderEmail, cleanPassword, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
                _logger.LogInformation("Email Sent: Delivered via SMTP to {RecipientEmail}.", recipientEmail);
                return new EmailResult(true, EmailDeliveryStatus.Sent);
            }

            return new EmailResult(false, EmailDeliveryStatus.Failed, "SMTP Connection Failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email Failed: SMTP exception for {RecipientEmail}: {ErrorMessage}", recipientEmail, ex.Message);
            return new EmailResult(false, EmailDeliveryStatus.Failed, ex.Message);
        }
    }

    private static string BuildHtmlBody(
        string recipientName,
        string businessName,
        string serviceName,
        DateTime appointmentStartTime,
        DateTime appointmentEndTime,
        string location,
        string cancellationPolicy)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f6f8; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; background: #ffffff; padding: 30px; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.05); margin: 0 auto; }}
        .header {{ border-bottom: 2px solid #6366f1; padding-bottom: 15px; margin-bottom: 20px; }}
        .title {{ color: #1e293b; margin: 0; font-size: 24px; }}
        .badge {{ display: inline-block; background-color: #dcfce7; color: #15803d; font-weight: 600; padding: 4px 12px; border-radius: 20px; font-size: 14px; margin-top: 10px; }}
        .details {{ background-color: #f8fafc; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #6366f1; }}
        .row {{ margin-bottom: 10px; font-size: 15px; color: #334155; }}
        .label {{ font-weight: 600; color: #475569; width: 140px; display: inline-block; }}
        .footer {{ font-size: 12px; color: #94a3b8; margin-top: 30px; border-top: 1px solid #e2e8f0; padding-top: 15px; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 class='title'>{businessName}</h1>
            <div class='badge'>✓ Booking Confirmed</div>
        </div>
        <p>Hi {recipientName},</p>
        <p>Great news! Your booking with <strong>{businessName}</strong> has been confirmed.</p>
        
        <div class='details'>
            <div class='row'><span class='label'>Service:</span> <strong>{serviceName}</strong></div>
            <div class='row'><span class='label'>Date & Time:</span> <strong>{appointmentStartTime:dddd, MMMM d, yyyy}</strong></div>
            <div class='row'><span class='label'>Time Slot:</span> <strong>{appointmentStartTime:h:mm tt} - {appointmentEndTime:h:mm tt}</strong></div>
            <div class='row'><span class='label'>Location:</span> {location}</div>
            {(string.IsNullOrWhiteSpace(cancellationPolicy) ? "" : $"<div class='row'><span class='label'>Cancellation:</span> {cancellationPolicy}</div>")}
        </div>

        <p>A Google Calendar invitation has been synchronized with the trainer's calendar.</p>
        <p>If you need to reschedule or have questions, feel free to reply directly to this email.</p>
        
        <div class='footer'>
            Powered by WorkPilot AI — Autonomous Lead Response &amp; Booking Agent
        </div>
    </div>
</body>
</html>";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CAMPAIGN EMAIL — AI-driven marketing campaigns
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<EmailResult> SendCampaignEmailAsync(
        string recipientEmail,
        string recipientName,
        string businessName,
        string subjectLine,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var provider = (_configuration["Email:Provider"] ?? "Simulated").Trim();
        var apiKey = _configuration["Email:ApiKey"];
        var senderEmail = _configuration["Email:SenderEmail"] ?? "onboarding@resend.dev";
        var senderName = _configuration["Email:SenderName"] ?? businessName;

        // Wrap plain text body in HTML if not already HTML
        var formattedBody = htmlBody.TrimStart().StartsWith("<")
            ? htmlBody
            : BuildCampaignHtmlBody(recipientName, businessName, subjectLine, htmlBody);

        // Simulated mode
        if (provider.Equals("Simulated", StringComparison.OrdinalIgnoreCase) ||
            ((provider.Equals("Resend", StringComparison.OrdinalIgnoreCase) || provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase)) &&
             (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_EMAIL_API_KEY_HERE" || apiKey.StartsWith("YOUR_"))))
        {
            _logger.LogInformation("Campaign Email Simulated: To {RecipientEmail} | Subject: {Subject}", recipientEmail, subjectLine);
            return new EmailResult(true, EmailDeliveryStatus.Simulated);
        }

        if (provider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
        {
            return await SendCampaignViaResendAsync(apiKey!, senderEmail, senderName, recipientEmail, subjectLine, formattedBody, cancellationToken);
        }

        if (provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase))
        {
            return await SendCampaignViaSendGridAsync(apiKey!, senderEmail, senderName, recipientEmail, subjectLine, formattedBody, cancellationToken);
        }

        _logger.LogInformation("Campaign Email Simulated: Provider '{Provider}' fallback.", provider);
        return new EmailResult(true, EmailDeliveryStatus.Simulated);
    }

    private async Task<EmailResult> SendCampaignViaResendAsync(
        string apiKey, string senderEmail, string senderName,
        string recipientEmail, string subjectLine, string htmlBody,
        CancellationToken cancellationToken)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                from = $"{senderName} <{senderEmail}>",
                to = new[] { recipientEmail },
                subject = subjectLine,
                html = htmlBody
            };

            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(req, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Campaign email sent via Resend to {Email} | Subject: {Subject}", recipientEmail, subjectLine);
                return new EmailResult(true, EmailDeliveryStatus.Sent);
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Campaign Resend failed {Status}: {Error}", response.StatusCode, error);
            return new EmailResult(false, EmailDeliveryStatus.Failed, $"Resend: {response.StatusCode} — {error}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Campaign Resend exception for {Email}", recipientEmail);
            return new EmailResult(false, EmailDeliveryStatus.Failed, ex.Message);
        }
    }

    private async Task<EmailResult> SendCampaignViaSendGridAsync(
        string apiKey, string senderEmail, string senderName,
        string recipientEmail, string subjectLine, string htmlBody,
        CancellationToken cancellationToken)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                personalizations = new[] { new { to = new[] { new { email = recipientEmail } } } },
                from = new { email = senderEmail, name = senderName },
                subject = subjectLine,
                content = new[] { new { type = "text/html", value = htmlBody } }
            };

            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(req, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Campaign email sent via SendGrid to {Email}", recipientEmail);
                return new EmailResult(true, EmailDeliveryStatus.Sent);
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return new EmailResult(false, EmailDeliveryStatus.Failed, $"SendGrid: {response.StatusCode} — {error}");
        }
        catch (Exception ex)
        {
            return new EmailResult(false, EmailDeliveryStatus.Failed, ex.Message);
        }
    }

    private static string BuildCampaignHtmlBody(string name, string businessName, string subject, string bodyText)
    {
        var lines = bodyText.Split('\n').Select(l => $"<p>{System.Net.WebUtility.HtmlEncode(l.Trim())}</p>");
        return $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><style>
body {{ font-family: Arial, sans-serif; background: #f8f9fa; margin: 0; padding: 20px; }}
.container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; padding: 40px; box-shadow: 0 2px 12px rgba(0,0,0,0.08); }}
.header {{ background: linear-gradient(135deg, #6366f1, #8b5cf6); color: white; border-radius: 8px; padding: 24px; margin-bottom: 24px; text-align: center; }}
.header h1 {{ margin: 0; font-size: 22px; }}
.body-text p {{ color: #374151; line-height: 1.6; }}
.footer {{ margin-top: 32px; padding-top: 16px; border-top: 1px solid #e5e7eb; color: #9ca3af; font-size: 12px; text-align: center; }}
</style></head>
<body>
<div class='container'>
  <div class='header'><h1>{System.Net.WebUtility.HtmlEncode(businessName)}</h1></div>
  <div class='body-text'>{string.Join("", lines)}</div>
  <div class='footer'>Sent by WorkPilot AI — Your AI Business Operator</div>
</div>
</body></html>";
    }
}
