# GitHub Copilot Repository-Level Instructions

This repository contains a specialized AI-assisted development framework to help GitHub Copilot and other developer tooling maintain, review, and extend **WorkPilot AI — Small Business Operating System**.

---

## 🤖 Specialized Development Agents

Individual instruction guidelines are available in [.github/copilot/agents/](file:///c:/Hackathon/.github/copilot/agents/):

1. **[business-goal-agent.md](file:///c:/Hackathon/.github/copilot/agents/business-goal-agent.md)**: Decomposes natural language owner goals into measurable objectives.
2. **[business-analyst-agent.md](file:///c:/Hackathon/.github/copilot/agents/business-analyst-agent.md)**: Analyzes snapshot metrics, revenue trends, and session history.
3. **[customer-growth-agent.md](file:///c:/Hackathon/.github/copilot/agents/customer-growth-agent.md)**: Manages lead segmentation, retention cohorts, and targeting.
4. **[marketing-agent.md](file:///c:/Hackathon/.github/copilot/agents/marketing-agent.md)**: Handles campaign templates, offer copywriting, and email dispatch logic.
5. **[booking-agent.md](file:///c:/Hackathon/.github/copilot/agents/booking-agent.md)**: Interfaces with slots calculated by working hours and Google Calendar.
6. **[revenue-optimization-agent.md](file:///c:/Hackathon/.github/copilot/agents/revenue-optimization-agent.md)**: Identifies pricing promotions and capacity optimizations.
7. **[operations-agent.md](file:///c:/Hackathon/.github/copilot/agents/operations-agent.md)**: Controls morning proactive alerts and AI operation logs.
8. **[architect-agent.md](file:///c:/Hackathon/.github/copilot/agents/architect-agent.md)**: Enforces Clean Architecture boundaries, DI, and patterns.
9. **[security-agent.md](file:///c:/Hackathon/.github/copilot/agents/security-agent.md)**: Manages credential hygiene, isolation, and LLM boundaries.
10. **[qa-agent.md](file:///c:/Hackathon/.github/copilot/agents/qa-agent.md)**: Directs unit and integration test coverage.
11. **[ux-design-agent.md](file:///c:/Hackathon/.github/copilot/agents/ux-design-agent.md)**: Protects user design aesthetics for non-technical owners.
12. **[hackathon-judge-agent.md](file:///c:/Hackathon/.github/copilot/agents/hackathon-judge-agent.md)**: Validates actual features, outcomes, and code integrity.

---

## ⚡ Common Development Rules

1. **Read Before Write**: Always inspect the existing components, tools, or templates before modifying them.
2. **Reuse booking infrastructure**: Never duplicate slot engines or calendar managers. Use the registered C# tools.
3. **Never bypass approval gates**: Side-effect operations must execute only upon explicit user command.
4. **No hard-coded metrics**: All analytics calculations must pull from SQL Server / InMemory database states.
5. **Verify builds & test suites**: Confirm `dotnet build` and `dotnet test` remain fully green after changes.
