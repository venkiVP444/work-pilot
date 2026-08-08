# WorkPilot AI — Hackathon Demonstration Guide

WorkPilot AI is an AI-operated Small Business Operating System designed for local service providers. It enables non-technical business owners to manage operations, schedule clients, and run marketing campaigns using natural language.

---

## Key Features

### 1. Multi-Business Onboarding and Switching
Owners can register multiple business profiles. Upon registration, default working hours are initialized automatically for all days of the week:
- Monday to Friday: 9:00 AM to 5:00 PM (15-minute buffer)
- Saturday and Sunday: 9:00 AM to 2:00 PM (15-minute buffer)

A dropdown menu in the dashboard allows switching between the default FitPro demo template and newly created profiles.

### 2. Tenant Isolation
All data is partitioned by business ID. Creating a customer, service, booking, or campaign under one business has no visibility or side effects on other businesses. Leads with duplicate email addresses registered under different businesses are created as isolated records.

### 3. Multi-Agent Orchestration
High-level objectives are decomposed and executed by a sequence of specialized agents:
- BusinessGoalAgent: Translates user commands into measurable goals.
- BusinessAnalystAgent: Audits current database snapshot metrics.
- CustomerGrowthAgent: Segments active versus inactive leads.
- MarketingAgent: Drafts personalized outreach templates and coordinates email dispatches.
- BookingAgent: Handles calendar scheduling recommendations.
- RevenueOptimizationAgent: Evaluates pricing structure and capacity yield.
- OperationsAgent: Logs status updates and logs morning brief summaries.

### 4. Human in the Loop Approval
Any action determined to have medium or high risk (such as bulk email campaigns) is queued for explicit owner approval before execution.

---

## Step by Step Demonstration Script

### 1. Dashboard Navigation
- Load http://localhost:4200/owner in the web browser.
- The dashboard displays the proactive Opportunities card, listing inactive customers and empty calendar slots.

### 2. Onboard a New Business
- Under the Settings tab, click the onboarding action to register a new business.
- Enter details:
  - Name: Alpha Studio
  - Description: Yoga and meditation studio
  - Location: Seattle
  - Contact Email: contact@alphastudio.com
- Select Alpha Studio from the header dropdown. All customer lists, requests, and metrics refresh to empty states.

### 3. Add Service
- Navigate to Services Setup and click Add Service.
- Create a service named "Yoga Flow" with a price of 60 dollars and duration of 60 minutes.

### 4. Open Customer Booking Page
- Click Customer Booking Page in the header. The URL resolves to /book/{AlphaStudioGuid}.
- The booking page lists only the "Yoga Flow" service. Select a slot, enter customer details (e.g. name and email), and submit.

### 5. Approve Booking Request
- Switch back to the Owner Dashboard for Alpha Studio.
- The request appears in the Booking Requests Queue. Click Approve to confirm the booking.

### 6. Verify FitPro Baseline Persistence
- Switch the active business back to FitPro Personal Training.
- Confirm all 50 pre-seeded demo customers, historical bookings, and opportunities remain intact.

---

## Verification Commands

To run all automated backend tests:
```bash
dotnet test WorkPilot.sln --no-restore
```

To compile the frontend application:
```bash
npm run build
```
