# WorkPilot AI — Hackathon Evidence

Competition: Build with Gemini XPRIZE  
Date: 2026-08-08  
Team: venkiVP444

---

## Category Claim: AI Business Operating System

WorkPilot AI is an AI-operated Small Business Operating System that enables non-technical small-business owners to operate their business through natural language.

---

## Evidence Checklist

### Gemini AI Integration
- Customer booking agent: GeminiAgentService.ProcessCustomerMessageAsync() — sends customer messages to Gemini 1.5 Flash in structured JSON mode
- Owner business agent: GeminiAgentService.ProcessOwnerIntentAsync() — sends full business context to Gemini for multi-agent reasoning
- Fallback mode: Full deterministic fallback implemented for demo without API key
- Model: gemini-1.5-flash
- Response format: application/json structured output enforced

### Multi-Agent Architecture
Files:
- src/WorkPilot.Application/Orchestration/AIBusinessOrchestrator.cs — Coordinates agent pipelines dynamically based on objectives.
- src/WorkPilot.Application/Agents/ — 7 specialized runtime agents (BusinessGoalAgent, BusinessAnalystAgent, CustomerGrowthAgent, MarketingAgent, BookingAgent, RevenueOptimizationAgent, OperationsAgent)
- src/WorkPilot.Application/Tools/ — 8 typed functional tools (Analytics, Customers, Bookings, Campaigns, Communications)
- .github/copilot/agents/ — 12 development-time Copilot instruction agents

Agents running dynamically:
- BusinessGoalAgent — Decomposes owner message intent into measurable objectives
- BusinessAnalystAgent — Generates strategic snapshot insights
- CustomerGrowthAgent — Targets client segments
- MarketingAgent — Personalized email copywriting and dynamic dispatch
- BookingAgent — Schedules slot bookings matching owner availability rules
- RevenueOptimizationAgent — Identifies calendar capacity yield and bundles
- OperationsAgent — Proactively logs morning alerts and brief cards

### Human-in-the-Loop Approval Gate
- Every action has a RiskLevel: Low / Medium / High
- Medium/High risk actions require explicit owner approval
- Action plan card appears in owner chat with Approve and Reject options
- ExecuteActionAsync is only called after owner clicks Approve
- Reject records ActionStatus.Rejected in database

### Autonomous Execution
When owner approves a campaign:
1. IdentifyTargetCustomersAsync — SQL query to DB, finds inactive customers
2. Loop sends personalized emails via IEmailService.SendCampaignEmailAsync
3. Campaign.EmailsSent, Campaign.EmailsFailed updated in DB
4. AIAgentAction.Status = Completed written with timestamp
5. Operations dashboard shows full audit trail

### Business Impact Measurement
- AIAgentAction.ActualRevenue — updated after execution
- AIAgentAction.ActualBookings — booking requests generated
- Campaign.BookingsConfirmed — confirmed bookings from campaign leads
- Campaign.RevenueGenerated — revenue tracked per campaign
- Enhanced metrics endpoint returns AIInfluencedRevenue

### Proactive AI
- Opportunities endpoint loaded on dashboard start
- AI-generated opportunity cards without owner prompting:
  - Inactive customers -> estimated reactivation revenue
  - Empty slots -> slot-fill revenue potential
  - Revenue decline -> analysis prompt

### Audit Trail / Transparency
- AI Operations log contains:
  - Which agents activated
  - Owner's original intent
  - AI reasoning summary
  - Estimated vs actual impact
  - Timestamps: created, approved, executed, completed

### Real Customer Data
- 50 customers seeded with realistic last-visit dates
- 18 historical confirmed bookings for revenue analytics

### All Original Features Preserved
- Customer booking chat -> AI slot proposal -> owner approval -> Google Calendar event -> email confirmation
- Integration tests: 16 integration tests all passing
- Fallback modes working (no Gemini key, no Calendar, no email configured)

---

## Disclosures

- Gemini API key: Must be configured in appsettings.Local.json
- Google Calendar: Has simulated fallback if OAuth is unconfigured
- Email delivery: Has simulated fallback mode
- Data: Seeded demo data
- Auth: None (known limitation for hackathon)

---

## Build and Run

```bash
# Backend
cd src/WorkPilot.Api
dotnet run

# Frontend
cd src/WorkPilot.Web
npm install
npm start

# Tests
dotnet test WorkPilot.sln
```

---

## Key Demo Flow

1. Open http://localhost:4200/owner
2. Dashboard loads: opportunities listed
3. Type: "I need to make 20% more profit this month"
4. AI responds with business analysis and action plan card
5. Click Approve and Execute
6. System executes campaign and logs audit trail
7. Business Insights tab metrics updated
