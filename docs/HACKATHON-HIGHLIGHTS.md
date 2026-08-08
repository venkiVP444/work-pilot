# WorkPilot AI — Hackathon Highlights and Demo Guide

WorkPilot AI transforms a standard booking platform into an AI-operated Small Business Operating System. It allows small business owners to run their business operations entirely using natural language.

---

## Key Value Proposition: The Natural Language Operating System

For local service providers, learning complex software is a barrier to growth. With WorkPilot AI, they describe their goals in plain English:
"I need to make 20% more profit this month."

The system automatically retrieves their business data, coordinates specialized agents, drafts a marketing campaign to reactivate inactive clients, estimates the bookings and revenue, asks for the owner's approval, and executes the campaign upon click.

---

## System Architecture Overview

WorkPilot AI implements a decoupled, multi-layered agent and tool pattern designed to scale:

- The Orchestration Layer: The central brain. It parses owner goals, determines which agent workflow to execute, and tracks approvals.
- Runtime Agents: Domain-specific runtime components (BusinessGoalAgent, BusinessAnalystAgent, CustomerGrowthAgent, MarketingAgent, BookingAgent, RevenueOptimizationAgent, OperationsAgent).
- Functional Tools: Reusable and unit-testable tools (GetBusinessSnapshotTool, GetInactiveCustomersTool, GetCustomerSegmentsTool, CreateCampaignTool, GetCampaignResultsTool, SendCampaignEmailTool).

---

## Safety and Trust: Human-in-the-Loop

To protect local businesses from automated failures, actions are classified by risk levels:
- Low Risk: Automatic execution (such as loading analytics).
- Medium/High Risk: Human-in-the-Loop approval gate.

---

## Audit Trail and Transparency (AI Operations)

Every action the AI proposes or runs is persisted in the database with an audit log. Owners can open the AI Operations Log to see:
- Which agents were coordinated.
- The original intent and the AI's step-by-step reasoning.
- Estimated bookings and revenue vs. actual execution metrics.

---

## Local Hackathon Database Seeding

To power the demo on a local developer machine, the database is initialized with a 50-customer dataset matching a realistic distribution:
- Active Customers: 20
- Inactive 30-59 days: 8
- Inactive 60-89 days: 12
- Inactive 90+ days: 10
- Historical Bookings: 18 completed bookings linked to active clients

---

## Step-by-Step Hackathon Demo Script (3 Minutes)

1. Dashboard Load:
   - Open http://localhost:4200/owner.
   - Note the Opportunities Card on the dashboard. AI has detected inactive customers and empty slots.
2. Goal Input:
   - Type in the AI Business Chat: "I need to make 20% more profit this month".
3. Multi-Agent Reasoning:
   - Show the agent list loading sequentially.
   - Highlight the strategic insight: Average order value is calculated as 85 dollars.
4. Review Draft:
   - Show the Action Plan Card generated:
     - Target Customers: 22 (inactive 60+ days)
     - Estimated Revenue: 340 dollars
     - Estimated Bookings: 4
     - Draft email body loaded automatically.
5. Approve and Execute:
   - Click Approve and Execute.
   - The status changes to Success. The orchestrator records the actual execution, triggers the campaign, and logs the completion.
6. Verify Audit Trail:
   - Navigate to the AI Operations tab.
   - Review the completed audit trail showing the precise timestamps of when the campaign was proposed, approved, and successfully executed.
