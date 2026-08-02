# WorkPilot AI — Autonomous Lead Response & Booking Agent

> **Build with Gemini XPRIZE Hackathon Submission**  
> **Category**: Small Business Services  
> **Target Audience**: Independent Fitness Trainers & Small Fitness Studios  

---

## ⚡ Executive Summary

**WorkPilot AI** is an AI-native lead response and booking agent for small fitness businesses. Small business owners often lose potential clients because they cannot respond instantly while leading training sessions. WorkPilot AI uses **Gemini AI** to understand natural language booking requests, detect missing information, calculate real available slots against working hours & Google Calendar Free/Busy intervals, and present candidate appointment options.

Once a customer selects a slot, WorkPilot AI creates a `BookingRequest` requiring **Owner Approval**. Upon approval, the system re-validates availability, creates a **real Google Calendar Event**, dispatches a **real confirmation email**, and records interaction audit telemetry for hackathon evidence.

---

## 🏗️ System Architecture

```text
                                  +---------------------------------------+
                                  |         Angular 18+ Frontend          |
                                  |  - Customer Standalone Booking Page   |
                                  |  - Owner Dashboard & Approval Portal  |
                                  +-------------------+-------------------+
                                                      |
                                                      | HTTPS / REST (JSON)
                                                      v
+---------------------------------------------------------------------------------------------------------------+
|                                          ASP.NET Core 8 Web API                                               |
|                                                                                                               |
|  +--------------------------+  +------------------------------+  +-----------------------------------------+  |
|  |      WorkPilot.Api       |  |    WorkPilot.Application     |  |            WorkPilot.Domain             |  |
|  | - Controllers            |  | - BookingOrchestratorService |  | - Business, Service, AvailabilityRule   |  |
|  | - Swagger & Health Check |  | - SlotCalculationEngine      |  | - Lead, Conversation, BookingRequest    |  |
|  | - CORS Middleware        |  | - DTOs & Use Cases           |  | - Booking, AIInteractionLog             |  |
|  +--------------------------+  +--------------+---------------+  +-----------------------------------------+  |
|                                               |                                                               |
|                                               v                                                               |
|                                +--------------+---------------+                                               |
|                                |   WorkPilot.Infrastructure   |                                               |
|                                | - WorkPilotDbContext (EF Core|                                               |
|                                | - GeminiAgentService         |                                               |
|                                | - GoogleCalendarService      |                                               |
|                                | - EmailService (HTTPS API / Resend / SendGrid) |                                |
|                                +--------------+---------------+                                               |
+-----------------------------------------------|---------------------------------------------------------------+
                                                |
              +---------------------------------+--------------------------------+
              |                                 |                                |
              v                                 v                                v
    +------------------+              +-------------------+             +-------------------+
    |    SQL Server    |              |  Gemini 1.5 API   |             |  Google Calendar  |
    |  (EF Core / DB)  |              | (Structured JSON) |             | (FreeBusy & Event)|
    +------------------+              +-------------------+             +-------------------+
```

---

## 🔒 Safety & Separation Principles

1. **Reasoning vs. Execution**: Gemini AI is strictly isolated to natural language understanding, intent classification, and slot explanation. Gemini **never** executes external actions or database mutations directly.
2. **Deterministic Backend**: The backend validates every AI output, enforces working hours, checks buffer times, queries real Google Calendar Free/Busy intervals, and manages side effects.
3. **Owner Approval Gatekeeper**: All customer bookings require explicit owner approval in the dashboard before calendar events or confirmation emails are executed.
4. **Slot Re-Validation**: Availability is re-checked at approval time to guarantee zero double-bookings.

---

## 🛠️ Prerequisites & Setup

* **.NET 8.0 SDK** (`dotnet --version` >= 8.0)
* **Node.js 20+** & **npm**
* **SQL Server LocalDB / Express** (or SQL Server container)

### Environment Configuration (`.env`)

Copy `.env.example` to `.env` or configure application parameters in `src/WorkPilot.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WorkPilotDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "gemini-1.5-flash"
  },
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET",
    "RedirectUri": "http://localhost:5050/api/calendar/callback"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "noreply@workpilot.ai",
    "SenderPassword": "YOUR_SMTP_APP_PASSWORD"
  }
}
```

---

## 🚀 Running Locally

### 1. Run ASP.NET Core Backend API (Port 5050)

```bash
cd c:\Hackathon
dotnet run --project src/WorkPilot.Api
```

* **Swagger OpenAPI Docs**: `http://localhost:5050/swagger`
* **Health Endpoint**: `http://localhost:5050/api/health`

### 2. Run Angular Frontend SPA (Port 4200)

```bash
cd c:\Hackathon\src\WorkPilot.Web
npm start
```

* **Owner Dashboard**: `http://localhost:4200/dashboard`
* **Customer Booking Page**: `http://localhost:4200/book/11111111-1111-1111-1111-111111111111`

---

## 🧪 Automated Testing

WorkPilot AI includes comprehensive unit and integration test suites covering the deterministic slot calculation engine, business rule constraints, and end-to-end API booking workflows.

Run all tests:

```bash
cd c:\Hackathon
dotnet test WorkPilot.slnx
```

---

## ☁️ Google Cloud Run Deployment

The ASP.NET Core backend is containerized via a multi-stage `Dockerfile` ready for deployment to **Google Cloud Run**.

```bash
# 1. Build & Push Image to Google Artifact Registry / Container Registry
gcloud builds submit --tag gcr.io/YOUR_PROJECT_ID/workpilot-api:latest .

# 2. Deploy to Cloud Run
gcloud run deploy workpilot-api \
  --image gcr.io/YOUR_PROJECT_ID/workpilot-api:latest \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars "Gemini__ApiKey=YOUR_KEY,Google__ClientId=YOUR_CLIENT_ID"
```

---

## 🎬 Step-by-Step Hackathon Judge Demonstration

1. **Open Owner Dashboard**: Navigate to `http://localhost:4200/dashboard`.
   * View configured business **FitPro Personal Training**, active services ($85 / 60-min session), and working hours (Mon-Fri 6-9 PM, Sat 8-12 PM).
2. **Open Customer Booking Page**: Click **"Open Public Booking Page"** or visit `http://localhost:4200/book/11111111-1111-1111-1111-111111111111`.
3. **Send Natural Language Inquiry**:
   * Type: *"Hi, I want to start personal training. Do you have anything this Saturday morning?"*
4. **AI Intent & Slot Reasoning**:
   * Gemini analyzes the message, matches **Personal Training Session**, detects Saturday morning preference, and returns structured JSON.
   * The backend queries Google Calendar Free/Busy and calculates available slots (e.g. `Sat, Aug 8 @ 8:00 AM - 9:00 AM`, `Sat, Aug 8 @ 11:30 AM - 12:30 PM`).
5. **Slot Selection & Customer Contact Capture**:
   * Click a candidate slot (e.g., `8:00 AM - 9:00 AM`).
   * Enter Customer Name: `John Doe`, Email: `johndoe@example.com`. Click **Submit Booking Request**.
6. **Owner Approval & Calendar Sync**:
   * Switch back to **Owner Dashboard** -> **Pending Requests**.
   * Click **✓ Approve & Add to Google Calendar**.
   * Backend re-validates calendar free/busy, creates a **real Google Calendar Event**, dispatches a **real confirmation email**, updates lead status to `Converted`, and logs metrics.
