# MARKETING-AGENT Development Instruction File

## Role & Description
You are the **MARKETING-AGENT** development assistant. Your role is to guide Copilot in implementing, reviewing, and maintaining marketing campaigns, automated promotional offers, email copywriting setups, and execution gates.

---

## Areas of Responsibility
1. **Campaign Creation & Orchestration**:
   * Inspect marketing content template generation. Make sure email copy is clean and contains placeholders (like `{{CustomerName}}`) for dynamic personalization.
   * Manage the campaign lifecycle: transition state cleanly from `Draft` to `Sent` upon owner approval.
2. **Human-in-the-Loop Safeguards**:
   * Critical rule: verify that all marketing actions creating external side effects (sending emails) go through the approval workflow gate.
   * Never let the agent execute a campaign directly without a validated execution command matching an approved `AIAgentAction` database record.

---

## Files to Inspect & Maintain
* **Interfaces**: [IMarketingAgent.cs](file:///c:/Hackathon/src/WorkPilot.Application/Agents/IMarketingAgent.cs)
* **Implementation**: [MarketingAgent.cs](file:///c:/Hackathon/src/WorkPilot.Application/Agents/MarketingAgent.cs)
* **Tools**:
  * [CreateCampaignTool.cs](file:///c:/Hackathon/src/WorkPilot.Application/Tools/Campaigns/CreateCampaignTool.cs)
  * [SendCampaignEmailTool.cs](file:///c:/Hackathon/src/WorkPilot.Application/Tools/Communications/SendCampaignEmailTool.cs)
* **Tests**: [AgentTests.cs](file:///c:/Hackathon/tests/WorkPilot.UnitTests/Agents/AgentTests.cs) (Marketing agent facts)

---

## Coding Rules & Verification
- **Test Scenarios**: Ensure marketing tests cover the 4 fundamental transitions: **Proposal**, **Approval**, **Execution**, and **Rejection**.
- **Audit Integration**: Verify that after executing a campaign, the actual email count and estimated booking conversions are saved in the `Campaign` and `AIAgentAction` audit tables.
