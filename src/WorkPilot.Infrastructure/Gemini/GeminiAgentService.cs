using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Infrastructure.Gemini;

public class GeminiAgentService : IGeminiAgentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAgentService> _logger;

    public GeminiAgentService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAgentService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GeminiStructuredResponse> ProcessCustomerMessageAsync(
        GeminiAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY_HERE")
        {
            _logger.LogInformation("Gemini API key is unconfigured. Utilizing deterministic AI agent fallback logic.");
            return GenerateFallbackResponse(request);
        }

        try
        {
            var prompt = BuildGeminiPrompt(request);
            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(requestUri, jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini API returned status code {StatusCode}: {ErrorText}. Falling back.", response.StatusCode, errText);
                return GenerateFallbackResponse(request);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseGeminiResponse(responseJson, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API. Falling back safely.");
            return GenerateFallbackResponse(request);
        }
    }

    private string BuildGeminiPrompt(GeminiAgentRequest request)
    {
        var servicesText = string.Join("\n", request.AvailableServices.Select(s => $"- ID: {s.Id}, Name: '{s.Name}', Duration: {s.DurationMinutes}m, Price: ${s.Price}"));
        var currentDateStr = DateTime.UtcNow.ToString("yyyy-MM-dd (dddd)");

        return $@"
System Instruction:
You are WorkPilot AI, an intelligent booking assistant for small local fitness businesses.
Today is {currentDateStr}.

Your task is to analyze the customer's message, understand intent, detect missing info, and return a structured JSON response.

Available Services:
{servicesText}

Customer Message: ""{request.CustomerMessage}""

JSON Response Format (Respond ONLY with valid JSON):
{{
  ""intent"": ""BookingRequest"" | ""GeneralQuestion"" | ""Unsupported"" | ""Unknown"",
  ""decision"": ""ProposeSlots"" | ""AskClarification"" | ""CreateBookingRequest"" | ""Reject"",
  ""selectedServiceName"": string or null,
  ""serviceId"": string (Guid) or null,
  ""datePreference"": string (e.g. ""Sunday"", ""Saturday"", ""Tomorrow"", ""2026-08-09"") or null,
  ""timePreference"": string (e.g. ""Morning"", ""6 PM"", ""After work"") or null,
  ""missingInformation"": [ list of strings if any ],
  ""assistantMessage"": string (Friendly, clear response to the customer),
  ""reasoningSummary"": string (Brief 1-sentence AI internal decision rationale)
}}
";
    }

    private GeminiStructuredResponse ParseGeminiResponse(string geminiApiOutput, GeminiAgentRequest request)
    {
        try
        {
            using var doc = JsonDocument.Parse(geminiApiOutput);
            var candidates = doc.RootElement.GetProperty("candidates");
            var firstContent = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(firstContent)) return GenerateFallbackResponse(request);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var raw = JsonSerializer.Deserialize<RawGeminiOutput>(firstContent, options);

            if (raw == null) return GenerateFallbackResponse(request);

            // Match ServiceId if missing
            Guid? matchedServiceId = raw.ServiceId;
            if (matchedServiceId == null && !string.IsNullOrWhiteSpace(raw.SelectedServiceName))
            {
                var matched = request.AvailableServices.FirstOrDefault(s => s.Name.Contains(raw.SelectedServiceName, StringComparison.OrdinalIgnoreCase));
                if (matched != null) matchedServiceId = matched.Id;
            }
            if (matchedServiceId == null && request.AvailableServices.Count == 1)
            {
                matchedServiceId = request.AvailableServices.First().Id;
            }

            IntentType intent = Enum.TryParse<IntentType>(raw.Intent, true, out var parsedIntent) ? parsedIntent : IntentType.BookingRequest;
            DecisionType decision = Enum.TryParse<DecisionType>(raw.Decision, true, out var parsedDecision) ? parsedDecision : DecisionType.ProposeSlots;

            return new GeminiStructuredResponse(
                Intent: intent,
                Decision: decision,
                SelectedServiceName: raw.SelectedServiceName ?? request.AvailableServices.FirstOrDefault()?.Name,
                ServiceId: matchedServiceId,
                DatePreference: raw.DatePreference,
                TimePreference: raw.TimePreference,
                MissingInformation: raw.MissingInformation ?? new List<string>(),
                AssistantMessage: raw.AssistantMessage ?? "I'd be happy to help you book your session!",
                ReasoningSummary: raw.ReasoningSummary ?? "Processed by Gemini AI Agent."
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse Gemini JSON response. Falling back.");
            return GenerateFallbackResponse(request);
        }
    }

    private GeminiStructuredResponse GenerateFallbackResponse(GeminiAgentRequest request)
    {
        var msg = request.CustomerMessage.ToLowerInvariant();
        var defaultService = request.AvailableServices.FirstOrDefault();

        // Check if message relates to fitness goals, dates, times, or booking
        bool containsGoalOrBooking = msg.Contains("personal training") || msg.Contains("book") ||
                                     msg.Contains("sunday") || msg.Contains("saturday") || msg.Contains("monday") ||
                                     msg.Contains("tuesday") || msg.Contains("wednesday") || msg.Contains("thursday") ||
                                     msg.Contains("friday") || msg.Contains("tomorrow") || msg.Contains("today") ||
                                     msg.Contains("morning") || msg.Contains("afternoon") || msg.Contains("evening") ||
                                     msg.Contains("weight") || msg.Contains("gain") || msg.Contains("loss") ||
                                     msg.Contains("gym") || msg.Contains("workout") || msg.Contains("session") ||
                                     msg.Contains("trainer") || msg.Contains("slot") || msg.Contains("time") ||
                                     msg.Contains("yes") || msg.Contains("sure") || msg.Contains("ok") || msg.Contains("please") ||
                                     msg.Contains("pm") || msg.Contains("am") || Regex.IsMatch(msg, @"\d");

        // Extract date preference if present across ALL days of the week & date formats
        string? datePref = null;
        if (msg.Contains("sunday")) datePref = "Sunday";
        else if (msg.Contains("saturday")) datePref = "Saturday";
        else if (msg.Contains("monday")) datePref = "Monday";
        else if (msg.Contains("tuesday")) datePref = "Tuesday";
        else if (msg.Contains("wednesday")) datePref = "Wednesday";
        else if (msg.Contains("thursday")) datePref = "Thursday";
        else if (msg.Contains("friday")) datePref = "Friday";
        else if (msg.Contains("tomorrow")) datePref = "Tomorrow";
        else if (msg.Contains("today")) datePref = "Today";
        else if (Regex.IsMatch(msg, @"\b(\d{1,2})\s*(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\b"))
        {
            var match = Regex.Match(msg, @"\b(\d{1,2})\s*(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\b");
            datePref = match.Value;
        }
        else if (Regex.IsMatch(msg, @"\b(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s*(\d{1,2})\b"))
        {
            var match = Regex.Match(msg, @"\b(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s*(\d{1,2})\b");
            datePref = match.Value;
        }
        else if (Regex.IsMatch(msg, @"\b\d{1,2}[/.-]\d{1,2}[/.-]\d{2,4}\b"))
        {
            var match = Regex.Match(msg, @"\b\d{1,2}[/.-]\d{1,2}[/.-]\d{2,4}\b");
            datePref = match.Value;
        }

        // Extract time preference if present
        string? timePref = null;
        if (msg.Contains("morning")) timePref = "Morning";
        else if (msg.Contains("afternoon")) timePref = "Afternoon";
        else if (msg.Contains("evening")) timePref = "Evening";
        else if (Regex.IsMatch(msg, @"\b\d{1,2}\s*(am|pm)\b"))
        {
            var match = Regex.Match(msg, @"\b\d{1,2}\s*(am|pm)\b");
            timePref = match.Value;
        }

        if (containsGoalOrBooking)
        {
            var serviceName = defaultService?.Name ?? "Personal Training Session";
            var msgText = datePref != null || timePref != null
                ? $"I'd be glad to schedule your {serviceName}! Here are open slots matching your request:"
                : $"That sounds like a great fitness goal! Our {serviceName} is ideal for that. Here are open slots for your session:";

            return new GeminiStructuredResponse(
                Intent: IntentType.BookingRequest,
                Decision: DecisionType.ProposeSlots,
                SelectedServiceName: serviceName,
                ServiceId: defaultService?.Id,
                DatePreference: datePref ?? "Sunday",
                TimePreference: timePref ?? "Morning",
                MissingInformation: new List<string>(),
                AssistantMessage: msgText,
                ReasoningSummary: "Deterministic fallback: Customer expressed fitness goal or slot request."
            );
        }

        return new GeminiStructuredResponse(
            Intent: IntentType.GeneralQuestion,
            Decision: DecisionType.AskClarification,
            SelectedServiceName: defaultService?.Name,
            ServiceId: defaultService?.Id,
            DatePreference: null,
            TimePreference: null,
            MissingInformation: new List<string> { "preferred date", "preferred time" },
            AssistantMessage: "Hi there! I'd love to help you get booked. Which service and preferred day/time work best for you?",
            ReasoningSummary: "Deterministic fallback: Clarification needed for booking details."
        );
    }

    private class RawGeminiOutput
    {
        public string? Intent { get; set; }
        public string? Decision { get; set; }
        public string? SelectedServiceName { get; set; }
        public Guid? ServiceId { get; set; }
        public string? DatePreference { get; set; }
        public string? TimePreference { get; set; }
        public List<string>? MissingInformation { get; set; }
        public string? AssistantMessage { get; set; }
        public string? ReasoningSummary { get; set; }
    }
}
