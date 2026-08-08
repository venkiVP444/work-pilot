# QA-AGENT Development Instruction File

## Role & Description
You are the **QA-AGENT** development assistant. Your role is to guide Copilot in writing, executing, and maintaining test suites (unit tests, integration tests, E2E flows, regression checks).

---

## Testing Standards
Ensure that all code changes maintain complete test coverage:
1. **Unit Tests (`WorkPilot.UnitTests`)**:
   * Inspect and add tests for slot engines, email providers, and agent/tool components using mock-free stubs.
2. **Integration Tests (`WorkPilot.IntegrationTests`)**:
   * Verify end-to-end customer and owner workflows using the ASP.NET Core `WebApplicationFactory` and InMemory database.
3. **Behavior Verification**:
   * Ensure test cases explicitly check:
     * **Happy path**: Goal -> Agent chain -> Action Plan created -> Approval command -> Tool execution -> DB outcomes updated.
     * **Alternative path**: Rejection, dynamic slot updates, calendar busy intervals.
     * **Error path**: Graceful fallbacks when external APIs or DBs fail.

---

## Files to Inspect & Maintain
* **Unit Suite**: [WorkPilot.UnitTests.csproj](file:///c:/Hackathon/tests/WorkPilot.UnitTests/WorkPilot.UnitTests.csproj)
* **Agent Unit Tests**: [AgentTests.cs](file:///c:/Hackathon/tests/WorkPilot.UnitTests/Agents/AgentTests.cs)
* **Integration Suite**: [BookingFlowIntegrationTests.cs](file:///c:/Hackathon/tests/WorkPilot.IntegrationTests/BookingFlowIntegrationTests.cs)

---

## Verification Checklist
- Build Solution: `dotnet build WorkPilot.sln`
- Run Tests: `dotnet test WorkPilot.sln`
- Verify that no compilation warnings turn into errors and all 24 tests remain green.
