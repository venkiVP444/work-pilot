using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;

namespace WorkPilot.Application.Agents;

public class BusinessGoalAgent : IBusinessGoalAgent
{
    private readonly IGeminiAgentService _geminiService;

    public BusinessGoalAgent(IGeminiAgentService geminiService)
    {
        _geminiService = geminiService;
    }

    public async Task<DecomposedGoalOutput> DecomposeGoalAsync(DecomposeGoalInput input, CancellationToken cancellationToken = default)
    {
        // Invoke Gemini interpreter
        var intentRequest = new OwnerIntentRequest(input.BusinessId, input.RawGoal, input.Snapshot);
        var response = await _geminiService.ProcessOwnerIntentAsync(intentRequest, cancellationToken);

        // Decompose the intent into distinct typed objectives
        var objectives = new List<BusinessObjective>();
        
        if (response.RecommendedActionType == "CreateCampaign")
        {
            objectives.Add(new BusinessObjective(
                ObjectiveType: "ReactivateCustomers",
                Description: response.WhyRecommended,
                TargetSegment: response.TargetSegment,
                ImpactEstimate: response.EstimatedRevenue
            ));
        }
        else if (response.RecommendedActionType == "FillEmptySlots")
        {
            objectives.Add(new BusinessObjective(
                ObjectiveType: "FillEmptySlots",
                Description: response.WhyRecommended,
                TargetSegment: response.TargetSegment,
                ImpactEstimate: response.EstimatedRevenue
            ));
        }
        else
        {
            objectives.Add(new BusinessObjective(
                ObjectiveType: "AnalyzeBusiness",
                Description: response.ReasoningSummary,
                TargetSegment: "All",
                ImpactEstimate: 0
            ));
        }

        return new DecomposedGoalOutput(objectives, response.ReasoningSummary);
    }
}
