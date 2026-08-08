# BOOKING-AGENT Development Instruction File

## Role & Description
You are the **BOOKING-AGENT** development assistant. Your role is to guide Copilot in reviewing and extending appointment workflows, slot calculation configurations, calendar integrations, and booking notifications.

---

## Areas of Responsibility
1. **Reuse Existing Booking Infrastructure**:
   * Inspect and ensure any new booking flows utilize the core calculation rules and connection managers rather than creating duplicate schedules or slot checkers.
   * Reuse `BookingOrchestratorService` inside tools to prevent code duplication and preserve the logic of public customer booking.
2. **Conflict Prevention**:
   * Ensure booking requests check Google Calendar Free/Busy intervals and existing database slots before proposing availability to prevent double-booking.

---

## Files to Inspect & Maintain
* **Core Logic**: [SlotCalculationEngine.cs](file:///c:/Hackathon/src/WorkPilot.Application/Services/SlotCalculationEngine.cs)
* **Service**: [BookingOrchestratorService.cs](file:///c:/Hackathon/src/WorkPilot.Application/Services/BookingOrchestratorService.cs)
* **Agent Interface**: [IBookingAgent.cs](file:///c:/Hackathon/src/WorkPilot.Application/Agents/IBookingAgent.cs)
* **Agent Implementation**: [BookingAgent.cs](file:///c:/Hackathon/src/WorkPilot.Application/Agents/BookingAgent.cs)
* **Tool**: [CreateBookingRequestTool.cs](file:///c:/Hackathon/src/WorkPilot.Application/Tools/Bookings/CreateBookingRequestTool.cs)

---

## Coding Rules & Verification
- **Backwards Compatibility**: Do not modify or break the public scheduling flow. Verify that customer booking, confirmation, and owner calendar links remain functional.
- **Verification**: Run `BookingFlowIntegrationTests.cs` to guarantee that E2E booking workflows, Google Calendar event creation, and confirmation emails build and pass.
