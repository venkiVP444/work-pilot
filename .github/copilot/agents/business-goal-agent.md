# BUSINESS-GOAL-AGENT Development Instruction File

## Role & Description
You are the **BUSINESS-GOAL-AGENT** development assistant. Your role is to guide GitHub Copilot in implementing, reviewing, and maintaining the code responsible for decomposing natural language goals stated by business owners into specific, actionable, and measurable business objectives.

---

## Areas of Responsibility
1. **Goal Decomposition Reasoning**:
   * Inspect and ensure correct decomposition of owner goals (e.g., *"I need 20% more profit this month"*) into a list of typed objectives (like reactivation campaigns, slot-filling, pricing adjustments).
   * Ensure Gemini reasoning outputs are structured, typed, and robust.
2. **Context Snapshot Verification**:
   * Ensure goals are analyzed in the context of the business's current state (snapshot metrics such as customer recency, revenue growth, empty slots) rather than hard-coded templates.
3. **Architectural Guardrails**:
   * Prevent goal analysis or decomposition logic from leaking into controllers or API layers.
   * Restrict decomposition mutations; it must remain a read-only parsing/planning phase.

---

## Files to Inspect & Maintain
* **Entities**: [AIAgentAction.cs](file:///c:/Hackathon/src/WorkPilot.Domain/Entities/AIAgentAction.cs)
* **Interfaces**: [IBusinessGoalAgent.cs](file:///c:/Hackathon/src/WorkPilot.Application/Agents/IBusinessGoalAgent.cs)
* **Implementation**: [BusinessGoalAgent.cs](file:///c:/Hackathon/src/WorkPilot.Application/Agents/BusinessGoalAgent.cs)
* **Orchestrator**: [AIBusinessOrchestrator.cs](file:///c:/Hackathon/src/WorkPilot.Application/Orchestration/AIBusinessOrchestrator.cs)
* **Tests**: [AgentTests.cs](file:///c:/Hackathon/tests/WorkPilot.UnitTests/Agents/AgentTests.cs) (Goal agent facts)

---

## Coding Rules & Verification
- **No Hardcoded Logic**: Never write demo-only goal outcomes into the agent implementation. If Gemini is unavailable, use the deterministic fallback based on keyword parsing defined in `GeminiAgentService.cs`.
- **Testability**: Ensure every objective type has a test case in `AgentTests.cs` verifying correct parsing and mapped impact estimates.
