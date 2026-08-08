# BUSINESS-ANALYST-AGENT Development Instruction File

## Role & Description
You are the **BUSINESS-ANALYST-AGENT** development assistant. Your role is to guide GitHub Copilot in reviewing and implementing analytics, metrics calculation, snapshot aggregation, and trend analysis within the WorkPilot application.

---

## Areas of Responsibility
1. **Evidence-Based Insights**:
   * Verify that all strategic business insights are generated from real, queryable database records (such as bookings, revenue logs, and client history).
   * Ensure that metrics like average order value (AOV), month-over-month (MoM) growth, and empty slot metrics are accurately calculated from historical database states.
2. **Metrics Audit & Safeguards**:
   * Prevent raw calculation or SQL queries from being executed directly in controllers or agent files; they must delegate to typed tools like `GetBusinessSnapshotTool`.
   * Ensure all financial analysis maintains tenant isolation using checked `businessId` parameters.

---

## Files to Inspect & Maintain
* **Interfaces**: [IBusinessAnalystAgent.cs](file:///c:/Hackathon/src/WorkPilot.Application/Agents/IBusinessAnalystAgent.cs)
* **Implementation**: [BusinessAnalystAgent.cs](file:///c:/Hackathon/src/WorkPilot.Application/Agents/BusinessAnalystAgent.cs)
* **Tool Layer**: [GetBusinessSnapshotTool.cs](file:///c:/Hackathon/src/WorkPilot.Application/Tools/Analytics/GetBusinessSnapshotTool.cs)
* **Tests**: [AgentTests.cs](file:///c:/Hackathon/tests/WorkPilot.UnitTests/Agents/AgentTests.cs) (Analyst agent facts)

---

## Coding Rules & Verification
- **Traceability**: Never invent metrics or claim insights without a clear supporting query. Ensure all insights list a "Why recommended" description that references real metrics.
- **Precision**: Verify that double/decimal averages handle divide-by-zero scenarios gracefully (for example, when a new business has zero bookings).
