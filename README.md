# WorkPilot AI — Autonomous Business Operating System

WorkPilot AI is an AI-operated Small Business Operating System that enables non-technical small-business owners to manage operations, schedule appointments, and execute campaigns entirely using natural language.

---

## 5-Minute Hackathon Setup

### Prerequisites
- Windows
- .NET 8.0 SDK
- Node.js 20+ and npm
- SQL Server LocalDB

Note: SQL Server LocalDB is the database engine required for the default local setup. Database creation, schema generation, and initial data seeding are completed automatically on the first backend startup.

### Installation and Run

1. Restore and start the ASP.NET Core backend API:
```bash
dotnet restore
dotnet build
dotnet run --project src/WorkPilot.Api
```
The backend API runs locally.

2. Install dependencies and start the Angular frontend application:
```bash
cd src/WorkPilot.Web
npm install
npm start
```
The frontend application runs at http://localhost:4200.

3. Open the Owner Dashboard:
http://localhost:4200/owner

---

## First-Run Expectation

Upon first backend startup, SQL Server LocalDB creates the WorkPilotDb database, applies the schema, and seeds the default business profile and demo data:

- Business: FitPro Personal Training
- Services: Personal Training Session and Fitness & Nutrition Assessment
- Availability Rules: Standard business operating rules
- 50 pre-seeded demo customers matching the following segment distribution:
  - 20 active customers
  - 8 inactive 30-59 days
  - 12 inactive 60-89 days
  - 10 inactive 90+ days
- 18 historical bookings connected to active customer profiles

---

## Hackathon Demo Flow

Follow these steps to demonstrate the multi-business and multi-agent capabilities of WorkPilot AI:

1. Open the Owner Dashboard at http://localhost:4200/owner.
2. Confirm that the default business profile "FitPro Personal Training" is loaded, displaying the seeded customers and Opportunities card (inactives and empty slots).
3. Open the AI Business Chat tab.
4. Enter the command:
"I need to make 20% more profit this month"
5. Observe the multi-agent execution chain status updates in real-time.
6. Verify that the system identifies the target inactive segment and displays the Action Plan Card.
7. Click Approve & Execute on the action plan card.
8. Verify that the campaign status changes to Success, actual outcomes are recorded, and confirmation logs are updated.
9. Navigate to the AI Operations tab to view the audit log details (agent chain steps, timestamps, estimated versus actual impact).
10. Under the Settings tab, register a new business profile:
  - Name: Alpha Studio
  - Description: Yoga and meditation studio
  - Location: Seattle
  - Contact Email: contact@alphastudio.com
11. Switch the header dropdown context to "Alpha Studio". Confirm that the metrics, snapshot, and chat logs are completely reset to empty states.
12. Under Services Setup, click Add Service to create "Yoga Flow" ($60, 60 minutes).
13. Click Customer Booking Page in the header. Select a slot, enter customer details (e.g. name and email), and submit.
14. Switch back to the dashboard, approve the booking request, and confirm it appears in the booking queue.
15. Switch the active business back to "FitPro Personal Training" and verify that all original demo data remains unchanged.
