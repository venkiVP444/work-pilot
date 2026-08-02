using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Infrastructure.Email;
using Xunit;

namespace WorkPilot.UnitTests.Services;

public class EmailServiceTests
{
    private readonly ILogger<EmailService> _logger = NullLogger<EmailService>.Instance;

    [Fact]
    public async Task SendBookingConfirmationEmailAsync_ShouldReturnSimulated_WhenProviderIsSimulated()
    {
        var config = new TestConfiguration(new Dictionary<string, string?>
        {
            {"Email:Provider", "Simulated"},
            {"Email:SenderEmail", "test@example.com"}
        });

        var httpClient = new HttpClient();
        var emailService = new EmailService(httpClient, config, _logger);

        var result = await emailService.SendBookingConfirmationEmailAsync(
            "customer@example.com",
            "Customer Name",
            "FitPro Gym",
            "Personal Training",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "123 Fitness St",
            "24h Policy");

        Assert.True(result.Success);
        Assert.Equal(EmailDeliveryStatus.Simulated, result.Status);
    }

    [Fact]
    public async Task SendBookingConfirmationEmailAsync_ShouldReturnSimulated_WhenApiKeyIsMissing()
    {
        var config = new TestConfiguration(new Dictionary<string, string?>
        {
            {"Email:Provider", "Resend"},
            {"Email:ApiKey", ""},
            {"Email:SenderEmail", "test@example.com"}
        });

        var httpClient = new HttpClient();
        var emailService = new EmailService(httpClient, config, _logger);

        var result = await emailService.SendBookingConfirmationEmailAsync(
            "customer@example.com",
            "Customer Name",
            "FitPro Gym",
            "Personal Training",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "123 Fitness St",
            "24h Policy");

        Assert.True(result.Success);
        Assert.Equal(EmailDeliveryStatus.Simulated, result.Status);
    }

    [Fact]
    public async Task SendBookingConfirmationEmailAsync_ShouldReturnSent_WhenResendApiReturns200()
    {
        var stubHandler = new StubHttpMessageHandler(HttpStatusCode.OK, "{\"id\":\"msg_12345\"}");
        var httpClient = new HttpClient(stubHandler);

        var config = new TestConfiguration(new Dictionary<string, string?>
        {
            {"Email:Provider", "Resend"},
            {"Email:ApiKey", "re_test_key_12345"},
            {"Email:SenderEmail", "test@example.com"}
        });

        var emailService = new EmailService(httpClient, config, _logger);

        var result = await emailService.SendBookingConfirmationEmailAsync(
            "customer@example.com",
            "Customer Name",
            "FitPro Gym",
            "Personal Training",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "123 Fitness St",
            "24h Policy");

        Assert.True(result.Success);
        Assert.Equal(EmailDeliveryStatus.Sent, result.Status);
    }

    [Fact]
    public async Task SendBookingConfirmationEmailAsync_ShouldReturnFailed_WhenResendApiReturns400()
    {
        var stubHandler = new StubHttpMessageHandler(HttpStatusCode.BadRequest, "{\"error\":\"Invalid recipient email\"}");
        var httpClient = new HttpClient(stubHandler);

        var config = new TestConfiguration(new Dictionary<string, string?>
        {
            {"Email:Provider", "Resend"},
            {"Email:ApiKey", "re_test_key_12345"},
            {"Email:SenderEmail", "test@example.com"}
        });

        var emailService = new EmailService(httpClient, config, _logger);

        var result = await emailService.SendBookingConfirmationEmailAsync(
            "customer@example.com",
            "Customer Name",
            "FitPro Gym",
            "Personal Training",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "123 Fitness St",
            "24h Policy");

        Assert.False(result.Success);
        Assert.Equal(EmailDeliveryStatus.Failed, result.Status);
        Assert.Contains("Invalid recipient email", result.ErrorMessage);
    }
}

public class TestConfiguration : IConfiguration
{
    private readonly Dictionary<string, string?> _data;

    public TestConfiguration(Dictionary<string, string?> data)
    {
        _data = data;
    }

    public string? this[string key]
    {
        get => _data.TryGetValue(key, out var val) ? val : null;
        set => _data[key] = value;
    }

    public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
    public IChangeToken GetReloadToken() => throw new NotImplementedException();
    public IConfigurationSection GetSection(string key) => throw new NotImplementedException();
}

public class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseContent;

    public StubHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
    {
        _statusCode = statusCode;
        _responseContent = responseContent;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent)
        };
        return Task.FromResult(response);
    }
}
