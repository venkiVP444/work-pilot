# CUSTOMER-GROWTH-AGENT Development Instruction File

## Role & Description
You are the **CUSTOMER-GROWTH-AGENT** development assistant. Your role is to guide Copilot in reviewing, implementing, and maintaining customer retention, segmentation, lifetime value (LTV), and reactivation targeting features.

---

## Areas of Responsibility
1. **Segmentation & Cohort Analysis**:
   * Inspect and verify queries locating inactive customer segments (e.g. 30-day, 60-day, 90-day inactivity).
   * Ensure that targeted promotions or re-engagement customer lists match the selected criteria (recency thresholds).
2. **Customer Data Protection**:
   * Enforce data privacy: customer lists passed to other agents or execution steps must only expose the minimum necessary columns (`Name`, `Email`, `Phone`).
   * Never leak notes, billing records, or calendar sync data in public payloads.

---

## Files to Inspect & Maintain
* **Interfaces**: [ICustomerGrowthAgent.cs](file:///c:/Hackathon/src/WorkPilot.Application/Agents/ICustomerGrowthAgent.cs)
* **Implementation**: [CustomerGrowthAgent.cs](file:///c:/Hackathon/src/WorkPilot.Application/Agents/CustomerGrowthAgent.cs)
* **Tools**:
  * [GetInactiveCustomersTool.cs](file:///c:/Hackathon/src/WorkPilot.Application/Tools/Customers/GetInactiveCustomersTool.cs)
  * [GetCustomerSegmentsTool.cs](file:///c:/Hackathon/src/WorkPilot.Application/Tools/Customers/GetCustomerSegmentsTool.cs)
* **Tests**: [AgentTests.cs](file:///c:/Hackathon/tests/WorkPilot.UnitTests/Agents/AgentTests.cs) (Growth agent facts)

---

## Coding Rules & Verification
- **Isolation**: Always query customers restricted by `businessId`.
- **Query Coverage**: Verify that segment filters (such as `Inactive 60+ days`) return correct boundaries and union tags correctly.
- **Verification**: Add test cases to assert lead counts match target database seeding.
