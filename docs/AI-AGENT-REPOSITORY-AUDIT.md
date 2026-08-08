# WorkPilot AI — Repository Audit & Architecture Report

Audit Date: 2026-08-08  
Auditor: WorkPilot AI Engineering Agent  
Repository: venkiVP444/work-pilot  
Hackathon: Build with Gemini XPRIZE

---

## Executive Summary

WorkPilot has been transformed from an AI-assisted booking intake tool into an AI Business Operating System. The core design principle: the owner states a goal -> the AI analyzes, plans, executes, and reports results.

---

## Repository Structure

```
WorkPilot/
├── src/
│   ├── WorkPilot.Domain/          # Entities, Enums
│   ├── WorkPilot.Application/     # Services, Interfaces, DTOs
│   ├── WorkPilot.Infrastructure/  # EF Core, Gemini, Email, Calendar
│   ├── WorkPilot.Api/             # ASP.NET Core 8 Web API
│   └── WorkPilot.Web/             # Angular 18 SPA
├── tests/
│   ├── WorkPilot.UnitTests/       # Slot engine, email service tests
│   └── WorkPilot.IntegrationTests/# Full booking flow E2E tests
└── docs/                          # Documentation files
```

Architecture Pattern: Clean Architecture (Domain -> Application -> Infrastructure -> API)

---

## Tech Stack

- Backend: ASP.NET Core 8.0
- ORM: Entity Framework Core 8.x
- Database: SQL Server / In-Memory
- AI: Google Gemini 1.5 Flash
- Calendar: Google Calendar API v3
- Email: Resend / SendGrid / SMTP
- Frontend: Angular 18+
- Tests: xUnit

---

## Domain Entities

### Core Booking
- Business: Tenant configuration, calendar settings, communication tone
- Service: Service catalog (name, price, duration)
- AvailabilityRule: Per-day working hours configuration
- Lead: Customer/prospect record with retention analytics fields
- Conversation: Chat session between customer and AI
- ConversationMessage: Individual messages in a conversation
- BookingRequest: AI-proposed appointment pending owner approval
- Booking: Confirmed appointment (Google Calendar event + email sent)
- AIInteractionLog: Log of AI calls (latency, model, token usage)

### AI Business OS
- AIAgentAction: Audit trail of every AI agent action
- Campaign: AI-created marketing campaign (target segment, email content, results)

Lead retention fields:
- LastVisitDate — when customer last visited
- TotalBookings — lifetime booking count
- TotalSpend — lifetime revenue from customer
- Tags — customer segments
- IsActive — whether customer is currently active

---

## AI Agent Architecture

### Multi-Agent System

1. Owner Natural Language Intent
2. AIBusinessOrchestrator
3. BusinessSnapshotDto (tool: GetBusinessSnapshotTool)
4. GeminiAgentService.ProcessOwnerIntentAsync()
5. Gemini 1.5 Flash / Deterministic Fallback
6. OwnerIntentResponse (typed)
7. Risk Level Assessment: Low / Medium / High
   - Low Risk -> Auto-execute (analytics)
   - Medium -> Create AIAgentAction -> Owner Approval Required
   - High -> Always confirm
8. ExecuteActionAsync()
   - ExecuteCampaignAsync() -> identify customers -> send emails -> log results
   - ExecuteAnalysisAsync() -> read-only analysis
9. AIAgentAction.Status = Completed
10. Campaign metrics updated

### Agents Implemented

- Orchestrator: Routes owner intent, builds context, coordinates
- BusinessAnalyst: Builds business snapshot, analyzes revenue/trends
- CustomerGrowth: Identifies inactive customers by segment
- Marketing: Creates and executes campaign emails
- RevenueOptimization: Identifies slot-fill opportunities
- Operations: Aggregates proactive morning brief notifications

---

## API Endpoints

### Owner AI
- POST /api/owner/{id}/chat: Owner chat -> AI response + action plan
- POST /api/owner/{id}/execute-action: Execute approved AI action
- POST /api/owner/{id}/reject-action/{actionId}: Reject proposed action
- GET /api/owner/{id}/opportunities: Proactive morning brief
- GET /api/owner/{id}/ai-operations: AI audit log
- GET /api/owner/{id}/snapshot: Business context snapshot
- GET /api/owner/{id}/metrics/enhanced: Revenue + AI impact metrics

### Customer Booking
- POST /api/customer/{id}/conversation/message: Customer AI chat
- POST /api/customer/{id}/booking-request: Create booking request
- POST /api/booking-requests/{id}/approve: Owner approves booking
- POST /api/booking-requests/{id}/reject: Owner rejects booking
- POST /api/booking-requests/{id}/retry-email: Retry failed email
- GET /api/businesses/{id}/...: Business, services, availability
- GET /api/metrics/{id}: Dashboard metrics

---

## Frontend (Angular 18)

### New Tabs (AI Business OS)
1. AI Business Chat: Owner interface with chat inputs, agent step chains, action plan cards.
2. Business Insights: KPI cards, customer segment bars, opportunity cards, campaign results.
3. AI Operations: Audit trail of every AI agent action.

---

## Gemini Integration

- Customer Booking Agent: Parses customer language into structured JSON.
- Owner Business Operator Agent: Injecting business metrics, returning recommended action types, copy content, estimated metrics. Has fallback matching for reactivation, slot-filling, and business analysis.

---

## Seed Data

- 50 customers seeded with realistic visit distribution.
- 18 historical confirmed bookings for revenue metrics.

---

## Test Coverage

- Unit: SlotCalculationEngine: Passed
- Unit: EmailService: Passed
- Integration: BookingFlow: 16 integration tests passed
- Total: All Pass

---

## Security Assessment

- Credentials in .gitignore: Covered
- AI -> DB access: Safe (no raw queries executed directly by LLM)
- Prompt injection: Mitigated
- Tenant isolation: Implemented
- High-risk action gates: Implemented
- Owner authentication: Not implemented (known limitation for hackathon demo)
