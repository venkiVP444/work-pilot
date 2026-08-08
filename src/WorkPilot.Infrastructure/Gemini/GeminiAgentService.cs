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

    // ─────────────────────────────────────────────────────────────────────────
    // CUSTOMER BOOKING AGENT (existing — unchanged)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<GeminiStructuredResponse> ProcessCustomerMessageAsync(
        GeminiAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY_HERE" || apiKey.StartsWith("YOUR_"))
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

    // ─────────────────────────────────────────────────────────────────────────
    // OWNER BUSINESS OPERATOR AGENT (new)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<OwnerIntentResponse> ProcessOwnerIntentAsync(
        OwnerIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY_HERE" || apiKey.StartsWith("YOUR_"))
        {
            _logger.LogInformation("Gemini API key not configured. Using deterministic owner intent fallback.");
            return GenerateOwnerFallbackResponse(request);
        }

        try
        {
            var prompt = BuildOwnerIntentPrompt(request);
            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(requestUri, jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini owner intent API returned {StatusCode}: {ErrorText}. Falling back.", response.StatusCode, errText);
                return GenerateOwnerFallbackResponse(request);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseOwnerIntentResponse(responseJson, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API for owner intent. Falling back safely.");
            return GenerateOwnerFallbackResponse(request);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OWNER INTENT PROMPT BUILDER
    // ─────────────────────────────────────────────────────────────────────────

    private string BuildOwnerIntentPrompt(OwnerIntentRequest request)
    {
        var snap = request.BusinessSnapshot;
        var revenueGrowth = snap.RevenueLastMonth > 0
            ? ((snap.RevenueThisMonth - snap.RevenueLastMonth) / snap.RevenueLastMonth * 100).ToString("F1")
            : "N/A";

        return $@"
You are WorkPilot AI — an autonomous AI Business Operator for small businesses.
You think like a highly experienced business consultant combined with an autonomous AI agent.
Today's date: {DateTime.UtcNow:yyyy-MM-dd (dddd)}.

BUSINESS CONTEXT — {snap.BusinessName}:
- Total Customers: {snap.TotalCustomers}
- Active Customers (visited in 30 days): {snap.ActiveCustomers}
- Inactive 30+ days: {snap.InactiveCustomers30Days}
- Inactive 60+ days: {snap.InactiveCustomers60Days}
- Inactive 90+ days: {snap.InactiveCustomers90Plus}
- Revenue This Month: ${snap.RevenueThisMonth:F2}
- Revenue Last Month: ${snap.RevenueLastMonth:F2}
- Revenue Growth: {revenueGrowth}%
- Bookings This Month: {snap.BookingsThisMonth}
- Bookings Last Month: {snap.BookingsLastMonth}
- Pending Booking Requests: {snap.PendingBookingRequests}
- Empty Slots This Week: {snap.EmptySlotsThisWeek}
- Average Order Value: ${snap.AverageOrderValue:F2}
- Top Services: {string.Join(", ", snap.TopServices)}

OWNER MESSAGE: ""{request.OwnerMessage}""

AVAILABLE ACTIONS you can recommend:
- IdentifyInactiveCustomers: Find and analyze inactive customers
- CreateCampaign: Create a personalized reactivation email campaign  
- FillEmptySlots: Target customers likely to book specific slots
- AnalyzeRevenue: Deep-dive revenue trends and opportunities
- GenerateOffer: Create a special offer for a customer segment
- AnalyzeBusiness: Comprehensive business health analysis

RISK LEVELS:
- Low: Analytics, insights, report generation (auto-execute)
- Medium: Campaigns, bulk emails, offers (require owner approval)
- High: Pricing changes, refunds, irreversible actions (always confirm)

Respond with a JSON object ONLY (no markdown fences):
{{
  ""activeAgents"": [""Orchestrator"", ""BusinessAnalyst"", ""CustomerGrowth""],
  ""reasoningSummary"": ""One sentence: what you analyzed and why."",
  ""assistantMessage"": ""Clear, friendly message to the owner. Be specific with numbers from the business context. Show you understand their business."",
  ""recommendedActionType"": ""CreateCampaign"",
  ""riskLevel"": ""Medium"",
  ""estimatedImpact"": ""15-20 new bookings, $1,275-$1,700 revenue"",
  ""estimatedRevenue"": 1500.00,
  ""estimatedBookings"": 17,
  ""targetCustomerCount"": {snap.InactiveCustomers60Days},
  ""whatWillHappen"": ""Step-by-step: what the AI will do if approved"",
  ""whyRecommended"": ""Why this is the highest-impact action right now"",
  ""campaignSubjectLine"": ""We miss you! Come back for a special session"",
  ""campaignEmailBody"": ""Dear {{CustomerName}}, it's been a while since your last session at {snap.BusinessName}. We'd love to see you back..."",
  ""campaignOfferDescription"": ""Personalized reactivation offer for customers inactive 60+ days"",
  ""targetSegment"": ""Inactive 60+ days""
}}
";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OWNER INTENT RESPONSE PARSER
    // ─────────────────────────────────────────────────────────────────────────

    private OwnerIntentResponse ParseOwnerIntentResponse(string geminiApiOutput, OwnerIntentRequest request)
    {
        try
        {
            using var doc = JsonDocument.Parse(geminiApiOutput);
            var candidates = doc.RootElement.GetProperty("candidates");
            var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(text)) return GenerateOwnerFallbackResponse(request);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var raw = JsonSerializer.Deserialize<RawOwnerIntentOutput>(text, options);

            if (raw == null) return GenerateOwnerFallbackResponse(request);

            return new OwnerIntentResponse(
                ActiveAgents: raw.ActiveAgents ?? ["Orchestrator", "BusinessAnalyst"],
                ReasoningSummary: raw.ReasoningSummary ?? "Analyzed business context.",
                AssistantMessage: raw.AssistantMessage ?? "I've analyzed your business and have recommendations.",
                RecommendedActionType: raw.RecommendedActionType ?? "AnalyzeBusiness",
                RiskLevel: raw.RiskLevel ?? "Medium",
                EstimatedImpact: raw.EstimatedImpact ?? "Impact estimated after analysis.",
                EstimatedRevenue: raw.EstimatedRevenue,
                EstimatedBookings: raw.EstimatedBookings,
                TargetCustomerCount: raw.TargetCustomerCount,
                WhatWillHappen: raw.WhatWillHappen ?? "AI will analyze and execute the recommended action.",
                WhyRecommended: raw.WhyRecommended ?? "This is the highest-impact opportunity identified.",
                CampaignSubjectLine: raw.CampaignSubjectLine ?? "A special message for you",
                CampaignEmailBody: raw.CampaignEmailBody ?? "We'd love to see you again!",
                CampaignOfferDescription: raw.CampaignOfferDescription ?? "Personalized offer",
                TargetSegment: raw.TargetSegment ?? "All customers"
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse Gemini owner intent JSON. Falling back.");
            return GenerateOwnerFallbackResponse(request);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OWNER INTENT DETERMINISTIC FALLBACK
    // ─────────────────────────────────────────────────────────────────────────

    private OwnerIntentResponse GenerateOwnerFallbackResponse(OwnerIntentRequest request)
    {
        var msg = request.OwnerMessage.ToLowerInvariant();
        var snap = request.BusinessSnapshot;

        // Detect intent from message keywords
        bool wantsRevenue = msg.Contains("profit") || msg.Contains("revenue") || msg.Contains("money") || msg.Contains("sales") || msg.Contains("earn") || msg.Contains("income") || msg.Contains("growth") || msg.Contains("%");
        bool wantsCustomers = msg.Contains("customer") || msg.Contains("client") || msg.Contains("inactive") || msg.Contains("lost") || msg.Contains("back") || msg.Contains("return") || msg.Contains("reactivat");
        bool wantsSlots = msg.Contains("slot") || msg.Contains("empty") || msg.Contains("appointment") || msg.Contains("booking") || msg.Contains("fill") || msg.Contains("calendar");
        bool wantsAnalysis = msg.Contains("how") || msg.Contains("why") || msg.Contains("performance") || msg.Contains("analyze") || msg.Contains("report") || msg.Contains("analytics");
        bool wantsCampaign = msg.Contains("campaign") || msg.Contains("email") || msg.Contains("message") || msg.Contains("send") || msg.Contains("offer") || msg.Contains("promotion");

        // Analysis / general question path
        if (wantsAnalysis || (!wantsRevenue && !wantsCustomers && !wantsSlots && !wantsCampaign))
        {
            decimal revenueGrowth = snap.RevenueLastMonth > 0
                ? (snap.RevenueThisMonth - snap.RevenueLastMonth) / snap.RevenueLastMonth * 100
                : 0;

            return new OwnerIntentResponse(
                ActiveAgents: ["Orchestrator", "BusinessAnalyst"],
                ReasoningSummary: "Owner requested business analysis. Compiling revenue, booking, and customer health metrics.",
                AssistantMessage: $"Here's your **business health summary** for {snap.BusinessName}:\n\n" +
                                  $"💰 **Revenue**: ${snap.RevenueThisMonth:F0} this month ({(revenueGrowth >= 0 ? "+" : "")}{revenueGrowth:F1}% vs last month)\n" +
                                  $"📅 **Bookings**: {snap.BookingsThisMonth} this month\n" +
                                  $"👥 **Customers**: {snap.TotalCustomers} total, {snap.ActiveCustomers} active\n" +
                                  $"⚠️ **Inactive**: {snap.InactiveCustomers60Days} customers haven't returned in 60+ days\n" +
                                  $"📆 **Empty slots**: {snap.EmptySlotsThisWeek} this week\n\n" +
                                  $"**Biggest opportunity**: Reactivating your {snap.InactiveCustomers60Days} inactive customers could generate significant revenue. Want me to create a campaign?",
                RecommendedActionType: "AnalyzeBusiness",
                RiskLevel: "Low",
                EstimatedImpact: "Business analysis complete",
                EstimatedRevenue: 0,
                EstimatedBookings: 0,
                TargetCustomerCount: 0,
                WhatWillHappen: "Analysis is complete — no further action required unless you request one.",
                WhyRecommended: "Understanding your business health is the foundation for making informed decisions.",
                CampaignSubjectLine: "",
                CampaignEmailBody: "",
                CampaignOfferDescription: "",
                TargetSegment: ""
            );
        }

        // Empty slots path
        if (wantsSlots && snap.EmptySlotsThisWeek > 0)
        {
            decimal avgOrder = snap.AverageOrderValue > 0 ? snap.AverageOrderValue : 85m;
            decimal estRevenue = snap.EmptySlotsThisWeek * avgOrder;

            return new OwnerIntentResponse(
                ActiveAgents: ["Orchestrator", "RevenueOptimization", "CustomerGrowth", "Marketing"],
                ReasoningSummary: $"Owner wants to fill {snap.EmptySlotsThisWeek} empty slots. Targeting customers who previously booked matching time windows.",
                AssistantMessage: $"You have **{snap.EmptySlotsThisWeek} empty slots** this week.\n\n" +
                                  $"If filled, that's **${estRevenue:F0}** in potential revenue.\n\n" +
                                  $"I can identify customers who have previously booked similar time windows and send them a personalized 'book now' message.\n\n" +
                                  $"Review the campaign below and approve to send.",
                RecommendedActionType: "FillEmptySlots",
                RiskLevel: "Medium",
                EstimatedImpact: $"{snap.EmptySlotsThisWeek} slots filled, ${estRevenue:F0} revenue",
                EstimatedRevenue: estRevenue,
                EstimatedBookings: snap.EmptySlotsThisWeek,
                TargetCustomerCount: snap.ActiveCustomers,
                WhatWillHappen: $"1. Identify {snap.EmptySlotsThisWeek} open slots this week\n2. Find customers who booked similar times previously\n3. Send slot-specific availability messages\n4. Track booking responses",
                WhyRecommended: "Unused appointment capacity is pure lost revenue. Targeted messages to repeat customers have a high conversion rate.",
                CampaignSubjectLine: "Your favorite time slot is available this week!",
                CampaignEmailBody: $"Hi {{CustomerName}},\n\nWe have some great availability this week at {snap.BusinessName} that matches your usual schedule.\n\nDon't miss out — grab your spot before it's gone!\n\nBook now and we'll see you soon.\n\nBest,\nThe {snap.BusinessName} Team",
                CampaignOfferDescription: $"Slot-fill campaign targeting {snap.ActiveCustomers} active customers",
                TargetSegment: "Active customers — slot notification"
            );
        }

        // Revenue growth goal — most powerful demo path
        if (wantsRevenue && snap.InactiveCustomers60Days > 0)
        {
            decimal avgOrder = snap.AverageOrderValue > 0 ? snap.AverageOrderValue : 85m;
            int estimated = (int)Math.Round(snap.InactiveCustomers60Days * 0.35);
            decimal estRevenue = estimated * avgOrder;

            return new OwnerIntentResponse(
                ActiveAgents: ["Orchestrator", "BusinessAnalyst", "CustomerGrowth", "Marketing"],
                ReasoningSummary: $"Owner wants revenue growth. Fastest lever: {snap.InactiveCustomers60Days} inactive customers (60+ days). Reactivation campaign estimated {estimated} bookings.",
                AssistantMessage: $"I've analyzed your business. The fastest path to more profit is **customer reactivation**.\n\n" +
                                  $"📊 **Business snapshot:**\n" +
                                  $"• {snap.InactiveCustomers60Days} customers haven't visited in 60+ days\n" +
                                  $"• {snap.EmptySlotsThisWeek} empty appointment slots this week\n" +
                                  $"• Your average order value is ${avgOrder:F0}/session\n\n" +
                                  $"💡 **Biggest opportunity:**\n" +
                                  $"Sending a personalized reactivation message to those {snap.InactiveCustomers60Days} inactive customers could bring back **{estimated} bookings** worth **${estRevenue:F0}** in revenue.\n\n" +
                                  $"I've prepared a campaign. Review it below and approve when ready.",
                RecommendedActionType: "CreateCampaign",
                RiskLevel: "Medium",
                EstimatedImpact: $"{estimated} bookings, ${estRevenue:F0} revenue",
                EstimatedRevenue: estRevenue,
                EstimatedBookings: estimated,
                TargetCustomerCount: snap.InactiveCustomers60Days,
                WhatWillHappen: $"1. Identify {snap.InactiveCustomers60Days} customers inactive 60+ days\n2. Generate personalized re-engagement emails\n3. Send via email (requires your approval)\n4. Track booking requests received\n5. Report revenue impact",
                WhyRecommended: $"Your inactive customers already know and trust your business. Re-engaging them costs nothing and has the highest ROI of any marketing action.",
                CampaignSubjectLine: $"We miss you at {snap.BusinessName} — come back for a special session",
                CampaignEmailBody: $"Dear {{CustomerName}},\n\nIt's been a while since your last session at {snap.BusinessName}, and we miss seeing you!\n\nWe wanted to personally reach out to invite you back. As one of our valued clients, we'd love to help you get back on track with your goals.\n\nWe have available slots this week — book your session today.\n\nLooking forward to seeing you again!\n\nWarm regards,\nThe {snap.BusinessName} Team",
                CampaignOfferDescription: $"Personal re-engagement message to {snap.InactiveCustomers60Days} customers inactive 60+ days",
                TargetSegment: "Inactive 60+ days"
            );
        }

        // Default — customer growth path
        decimal defaultAvg = snap.AverageOrderValue > 0 ? snap.AverageOrderValue : 85m;
        int defaultEst = Math.Max(1, (int)(snap.InactiveCustomers60Days * 0.3));
        decimal defaultRev = defaultEst * defaultAvg;

        return new OwnerIntentResponse(
            ActiveAgents: ["Orchestrator", "CustomerGrowth", "Marketing"],
            ReasoningSummary: "Defaulting to highest-impact opportunity: inactive customer reactivation campaign.",
            AssistantMessage: $"I've identified your biggest opportunity: **{snap.InactiveCustomers60Days} customers** who haven't returned in 60+ days.\n\n" +
                              $"A personalized reactivation message could bring back **{defaultEst} customers**, generating **${defaultRev:F0}** in revenue.\n\n" +
                              $"Shall I prepare the campaign?",
            RecommendedActionType: "CreateCampaign",
            RiskLevel: "Medium",
            EstimatedImpact: $"{defaultEst} bookings, ${defaultRev:F0} revenue",
            EstimatedRevenue: defaultRev,
            EstimatedBookings: defaultEst,
            TargetCustomerCount: snap.InactiveCustomers60Days,
            WhatWillHappen: $"1. Identify inactive customers\n2. Create personalized emails\n3. Send campaign (after your approval)\n4. Track and report results",
            WhyRecommended: "Re-engaging lapsed customers is the fastest and cheapest way to grow revenue.",
            CampaignSubjectLine: $"We miss you at {snap.BusinessName}",
            CampaignEmailBody: $"Dear {{CustomerName}},\n\nWe miss you at {snap.BusinessName}! It's been a while since your last visit, and we'd love to welcome you back.\n\nWe have availability this week — book your session today.\n\nSee you soon!\nThe {snap.BusinessName} Team",
            CampaignOfferDescription: $"Reactivation message for {snap.InactiveCustomers60Days} inactive customers",
            TargetSegment: "Inactive 60+ days"
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CUSTOMER BOOKING — PROMPT BUILDER (unchanged from original)
    // ─────────────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────────
    // CUSTOMER BOOKING — RESPONSE PARSER (unchanged from original)
    // ─────────────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────────
    // CUSTOMER BOOKING — DETERMINISTIC FALLBACK (unchanged from original)
    // ─────────────────────────────────────────────────────────────────────────

    private GeminiStructuredResponse GenerateFallbackResponse(GeminiAgentRequest request)
    {
        var msg = request.CustomerMessage.ToLowerInvariant();
        var defaultService = request.AvailableServices.FirstOrDefault();

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

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE INNER CLASSES
    // ─────────────────────────────────────────────────────────────────────────

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

    private class RawOwnerIntentOutput
    {
        public string[]? ActiveAgents { get; set; }
        public string? ReasoningSummary { get; set; }
        public string? AssistantMessage { get; set; }
        public string? RecommendedActionType { get; set; }
        public string? RiskLevel { get; set; }
        public string? EstimatedImpact { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public int EstimatedBookings { get; set; }
        public int TargetCustomerCount { get; set; }
        public string? WhatWillHappen { get; set; }
        public string? WhyRecommended { get; set; }
        public string? CampaignSubjectLine { get; set; }
        public string? CampaignEmailBody { get; set; }
        public string? CampaignOfferDescription { get; set; }
        public string? TargetSegment { get; set; }
    }
}
