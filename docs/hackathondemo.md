# WorkPilot AI — Project Guide and Demonstration Script

WorkPilot AI is an AI-operated Small Business Operating System that allows non-technical service business owners to manage operations, client bookings, and marketing campaigns using natural language.

---

## Executive Summary

Independent service providers often struggle to respond to client inquiries while delivering services, leading to lost revenue. WorkPilot AI solves this by deploying a team of specialized AI agents that run the business autonomously. 

The system validates all AI outputs, checks real calendar availability, and processes customer booking requests. All actions require explicit owner approval before dispatching emails or creating Google Calendar events.

---

## System Architecture

WorkPilot AI utilizes a multi-agent system implemented in ASP.NET Core:

### 1. Multi-Agent Runtimes
- BusinessGoalAgent: Translates owner messages into actionable goals.
- BusinessAnalystAgent: Computes business metrics (revenue, customer counts, empty slots).
- CustomerGrowthAgent: Analyzes database segments to find target opportunities.
- MarketingAgent: Writes personalized campaign email copy.
- BookingAgent: Handles incoming customer messages and coordinates availability calculation.
- RevenueOptimizationAgent: Models financial impact and outcomes.
- OperationsAgent: Logs execution telemetry and creates auditable records.

### 2. Dynamic Orchestration
The AIBusinessOrchestrator manages the agent pipeline. When the owner submits a query, the orchestrator routes the request sequentially across agents to build a structured action plan.

### 3. Decoupled Typed Tools
Agents interact with database and communication channels using isolated, typed tools:
- GetBusinessSnapshotTool
- GetCustomerSegmentsTool
- GetInactiveCustomersTool
- GetEmptySlotsTool
- CreateCampaignTool
- SendCampaignEmailTool
- CreateBookingRequestTool

---

## Core Features

### 1. Multi-Business Onboarding and switching
Owners can register new business profiles. Upon registration, default weekly availability hours are initialized automatically:
- Monday to Friday: 9:00 AM to 5:00 PM (15-minute buffer)
- Saturday and Sunday: 9:00 AM to 2:00 PM (15-minute buffer)

A dropdown menu in the header allows the owner to switch between different business contexts instantly.

### 2. Tenant Isolation
All database tables enforce partition boundaries by BusinessId. Leads, availability rules, services, bookings, and campaign logs are isolated. Creating or deleting records in one business profile has no effect on other profiles.

### 3. LocalDB Portability and Startup Seeding
The database configuration points to standard SQL Server LocalDB. On application startup, the system automatically checks if the database exists, creates it, applies the schema, and seeds a 50-customer and 18-booking demo dataset. The seeding process is fully idempotent and does not create duplicate records on subsequent startups.

### 4. Resilient Fallbacks
If API credentials are empty or contain placeholder values (starting with YOUR_), the system operates in simulated mode:
- EmailService: Logs messages and returns simulated status.
- GoogleCalendarService: Computes scheduling rules and returns simulated event IDs.
- GeminiAgentService: Uses deterministic fallbacks matching target keywords.

---

## Hackathon Demonstration Script

Follow these steps to demonstrate the full capabilities of WorkPilot AI:

### Step 1: Open the Owner Dashboard
1. Navigate to http://localhost:4200/owner.
2. Verify that the default profile "FitPro Personal Training" is active.
3. Review the seeded database metrics, including the 50 customers and 18 historical bookings.

### Step 2: Run the Profit Campaign
1. Open the AI Business Chat tab.
2. Submit the command: "I need to make 20% more profit this month".
3. Watch the agent execution chain status updates in real-time.
4. Verify that the system recommends targeting the "Inactive 60+ days" segment and displays the Action Plan Card.
5. Click Approve & Execute on the action plan card.
6. Verify that the campaign logs update and record estimated versus actual revenue impact.
7. Open the AI Operations tab to audit the telemetry logs.

### Step 3: Register a New Business
1. Under the Settings tab, register a new business profile named "Alpha Studio".
2. Switch the dropdown header context to "Alpha Studio".
3. Verify that all metrics, logs, and opportunities reset to empty states.
4. Under Services Setup, add a new service named "Yoga Flow" ($60, 60 minutes).
5. Switch to the Availability tab and confirm that the default business hours (9 AM to 5 PM weekdays, 9 AM to 2 PM weekends) are loaded.

### Step 4: Book an Appointment
1. Click Customer Booking Page in the header dropdown.
2. Select an available slot.
3. Enter customer name and email, and submit the booking request.
4. Return to the dashboard under Pending Requests.
5. Click Approve & Add to Google Calendar.
6. Verify that the booking request is processed and added to the booking history list.
7. Switch back to "FitPro Personal Training" and verify that all original demo records remain untouched.

---

## Validation Status

- Unit Tests: 14 passing.
- Integration Tests: 16 passing.
- Frontend Build: Angular production build compiles with no errors.
