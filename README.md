# ai-agent-en
**Enterprise Agent Runtime + Governance Fabric**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
![Stars](https://img.shields.io/github/stars/Plillie96/ai-agent-en?style=social)

**The production-grade backbone for autonomous agents at enterprise scale.**

While everyone else is still playing in Python sandboxes, ai-agent-en gives you **secure, auditable, policy-enforced runtime** built natively on .NET -- designed to become the **Autonomous Operations Layer** that watches enterprise systems, decides what needs to happen, executes inside those systems, documents everything, and proves financial impact.

> Most companies are building agents.
> Almost nobody is building the **operating system** for enterprise agents.
> This is that operating system.

---

## What It Actually Does

This is not chat. Not copilots. Not dashboards.

It is the system that:

- **Watches** enterprise systems for events and anomalies
- **Decides** what needs to happen using rules + LLM reasoning
- **Executes** inside those systems via real API connectors
- **Documents** every action with full governance audit trail
- **Proves** financial impact in real time

All within policy guardrails. No human intervention required unless risk threshold exceeded.

---

## Live Scenarios

### Sales -- Stalled Deal Recovery
At 3:17 AM, the platform:

1. Detects a $480K deal stalled 11 days
2. Reads last call transcript, identifies pricing objection + missing security doc
3. Generates revised pricing within approved thresholds (10% discount, $432K)
4. Sends SOC2 report, security whitepaper, and DPA automatically
5. Books follow-up meeting, updates Salesforce, notifies rep with summary

**No rep intervention required.**

### Finance -- Month-End Reconciliation
At month-end:
- Detects revenue recognition inconsistencies across 142 contracts
- Cross-checks contract clauses, adjusts booking classification
- Flags 3 risky entries for human review
- Prepares board-ready variance explanation with EBITDA impact

**Finance reviews, not builds.**

### Legal -- Contract Review
When a new contract comes in:
- Classifies contract type, extracts 24 clauses
- Compares against playbook, identifies 3 non-standard terms
- Redlines liability cap, data processing, and IP scope
- Routes only high-risk clauses to human counsel, auto-executes standard deals

**60-80% reduction in manual review.**

### IT / Security -- Incident Response
When abnormal access pattern detected:
- Correlates identity + behavior logs across Azure AD, SharePoint, GitHub
- Scores risk at 85/100, temporarily restricts access
- Opens P1 ServiceNow ticket, generates forensic report
- Notifies CISO dashboard with remediation steps

**Fully logged and reversible.**

### Procurement -- Vendor Negotiation
Vendor pricing increases 9%:
- Compares historical benchmarks (industry median: 4.2%)
- Checks contract escalation clause (cap: 5%)
- Generates counterproposal at 5%, sends negotiation email
- Calculates $40K savings impact, escalates if rejection occurs

**Procurement becomes strategic, not clerical.**

### HR -- Attrition Prevention
Attrition probability spikes in high-performing engineering team:
- Detects sentiment drop (-1.4 over 90 days)
- Identifies comp is 12% below market for Sr. Engineers
- Recommends retention packages within policy ($96K investment vs $720K replacement)
- Schedules manager check-ins, tracks 30/60/90 day KPIs

**Proactive retention instead of exit interviews.**

---

## Architecture

```
EnterpriseAgentPlatform.slnx
src/
  Platform.Core/                  Domain models and interfaces
    Agents/                       IAgent, AgentContext, AgentResult, InputHelper
    Events/                       SystemEvent, IEventBus
    Governance/                   GovernancePolicy, IPolicyEngine, IAuditLog
    Impact/                       ImpactMetrics, IImpactTracker, ImpactSummary
    Integration/                  IEnterpriseConnector
    Workflows/                    WorkflowDefinition, WorkflowInstance

  Platform.Orchestration/         Multi-agent workflow engine
    Engine/
      WorkflowEngine              Sequential steps, governance checks, timeouts
      EventDrivenOrchestrator     Events to workflow trigger matching
      OrchestratorBackgroundService  IHostedService lifecycle
    Registry/                     AgentRegistry, WorkflowRegistry

  Platform.Governance/            Trust and compliance layer
    Engine/                       RuleBasedPolicyEngine
    Audit/                        InMemoryAuditLog
    Persistence/                  EfCoreAuditLog, ScopedAuditLog, GovernanceDbContext

  Platform.Intelligence/          Decision and reasoning layer
    Reasoning/                    IReasoningEngine, AzureOpenAIReasoningEngine
    Detection/                    AnomalyDetector, DetectionRule

  Platform.Impact/                Financial impact tracking
    Tracking/                     InMemoryImpactTracker
    Persistence/                  EfCoreImpactTracker, ScopedImpactTracker, ImpactDbContext

  Platform.Integration/           Enterprise system connectors
    Connectors/
      SalesforceConnector         Simulated (dev/test)
      SalesforceHttpConnector     Real OAuth2 + REST API v59.0
      ServiceNowConnector         Simulated (dev/test)
      ServiceNowHttpConnector     Real Basic Auth + Table API
    Events/                       InMemoryEventBus (Channel-based)

  Platform.Runtime/               API host + dashboard
    Agents/
      Sales/                      DealAnalysis, Pricing, Document, FollowUp
      Finance/                    RevenueRecognition, RiskFlagging
      Legal/                      ContractClassification, ContractRedline
      IT/                         SecurityResponse, Ticketing
      Procurement/                VendorNegotiation, ProcurementComms
      HR/                         AttritionDetection, RetentionAction
    Setup/                        PlatformBootstrap (agents, workflows, policies)
    wwwroot/                      Dashboard UI (HTML/CSS/JS)
    Program.cs                    API endpoints + DI wiring

tests/
  Platform.Tests/                 27 tests
```

---

## Key Capabilities

| Capability | Implementation |
|---|---|
| **Multi-agent orchestration** | Sequential steps, input mapping, conditional execution, timeouts, failure routing |
| **Governance enforcement** | Rule-based policy engine with action patterns, parameter thresholds, department scoping |
| **Full audit trail** | Every step logged: agent, action, outcome, policy, justification -- persisted to SQLite |
| **Event-driven triggers** | System events auto-match to workflows and execute autonomously |
| **Financial impact tracking** | Cost saved, revenue influenced, time saved -- per workflow, per department, real-time |
| **Human-in-the-loop** | Steps can require approval, policies can escalate, governance gates at every step |
| **Azure OpenAI integration** | IReasoningEngine with Azure OpenAI SDK -- config-driven, optional |
| **Real enterprise connectors** | Salesforce (OAuth2 + REST), ServiceNow (Basic Auth + Table API) -- with simulated fallbacks |
| **Persistent storage** | EF Core + SQLite with scoped-to-singleton bridging pattern |
| **Background processing** | IHostedService-based orchestrator with auto-restart on failure |
| **Dashboard UI** | Real-time dark-themed SPA -- agents, workflows, audit, events, impact metrics |

---

## Governance Model

Every agent action passes through the governance fabric before execution:

```
Event Detected
    |
    v
Workflow Triggered
    |
    v
+-- For Each Step ----------------+
|   Policy Engine evaluates:      |
|   - Action patterns (regex)     |
|   - Parameter thresholds        |
|   - Agent identity              |
|   - Department scope            |
|                                 |
|   Decision:                     |
|   ALLOW     -> Execute agent    |
|   DENY      -> Block + log      |
|   REQUIRE_APPROVAL -> Pause     |
|   AUDIT     -> Log + continue   |
|   ALERT     -> Notify + cont.   |
|                                 |
|   Audit Entry recorded          |
+---------------------------------+
    |
    v
Impact Metrics calculated
```

### Built-in Policies

| Policy | Department | Rule |
|---|---|---|
| Global Audit Trail | All | Audit every agent action |
| Financial Transfer Guard | All | Block automated financial transfers |
| Sales Discount Threshold | Sales | Require approval for discounts > 20% |
| Revenue Reclassification | Finance | Require human review for reclassification |
| High-Risk Contract Escalation | Legal | Escalate contracts with risk score > 7.0 |
| Security Action Alert | IT | Alert on all security incident response actions |
| High-Value Negotiation | Procurement | Require approval for counter-offers > $50K impact |
| Retention Package Approval | HR | Require VP approval for retention packages |

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run

```bash
git clone https://github.com/Plillie96/ai-agent-en.git
cd ai-agent-en
dotnet build
dotnet test
dotnet run --project src/Platform.Runtime/Platform.Runtime.csproj
```

Open **http://localhost:5059** for the dashboard.

### Execute Your First Workflow

```bash
curl -X POST http://localhost:5059/api/workflows/sales-stalled-deal-recovery/execute \
  -H "Content-Type: application/json" \
  -d '{"dealId":"OPP-48291","daysStalled":11,"amount":480000}'
```

### Publish an Event (Triggers Workflow Automatically)

```bash
curl -X POST http://localhost:5059/api/events \
  -H "Content-Type: application/json" \
  -d '{"source":"Salesforce","eventType":"sales.opportunity.stalled","department":"Sales","severity":2,"payload":{"dealId":"OPP-99182","daysStalled":14,"amount":320000}}'
```

---

## API Reference

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/health` | Health check |
| `GET` | `/api/agents` | List all 14 registered agents |
| `GET` | `/api/workflows` | List all 6 workflow definitions |
| `POST` | `/api/workflows/{id}/execute` | Execute a workflow with JSON inputs |
| `POST` | `/api/events` | Publish a system event |
| `GET` | `/api/audit` | Query audit trail (filter by agent, workflow) |
| `GET` | `/api/impact` | Get impact summary (filter by department) |
| `GET` | `/api/impact/dashboard` | Board-level metrics dashboard |
| `POST` | `/api/governance/evaluate` | Test a policy evaluation |
| `POST` | `/api/intelligence/reason` | Send reasoning request to Azure OpenAI |
| `GET` | `/api/connectors/health` | Enterprise connector health check |

---

## Configuration

All configuration is in `appsettings.json`:

```json
{
  "Storage": {
    "Mode": "Sqlite"
  },
  "AzureOpenAI": {
    "Endpoint": "",
    "ApiKey": "",
    "DeploymentName": "gpt-4o"
  },
  "Salesforce": {
    "InstanceUrl": "",
    "ClientId": "",
    "ClientSecret": "",
    "Username": "",
    "Password": ""
  },
  "ServiceNow": {
    "InstanceUrl": "",
    "Username": "",
    "Password": ""
  }
}
```

When credentials are empty, the platform uses simulated connectors -- perfect for development and demos. Fill them in for production.

---

## Measurable Outcomes

Within 6-12 months of deployment:

| Metric | Target |
|---|---|
| Manual workflow reduction | 20-40% |
| Deal cycle acceleration | 15-25% faster |
| Procurement savings | 10-20% |
| Audit prep reduction | 30-50% |
| Risk exposure | Measurable decline |

---

## The Big Shift

**Today:** Humans operate systems.

**With this:** Systems operate themselves. Humans supervise.

After 12-18 months, the platform has:
- Cross-system visibility
- Institutional memory
- Decision history
- Economic feedback loops

It becomes the most informed "employee" in the company. And it never leaves.

---

## What It Will NOT Do

- Fully replace executives
- Operate without guardrails
- Run unsupervised in high-risk domains
- Eliminate humans entirely

## What It WILL Do

- Remove repetitive execution
- Accelerate decision cycles
- Reduce operational leakage
- Increase consistency
- Surface strategic insights

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10, ASP.NET Core Minimal APIs |
| Storage | EF Core + SQLite (swappable) |
| Intelligence | Azure OpenAI SDK (Azure.AI.OpenAI) |
| Connectors | HttpClient + typed clients |
| Testing | xUnit (27 tests) |
| UI | Vanilla HTML/CSS/JS SPA |

---

## Project Stats

- **14** autonomous agents across 6 departments
- **6** multi-step orchestrated workflows
- **8** governance policies with department-scoped rules
- **27** automated tests (engine, policies, persistence, events, workflows)
- **11** API endpoints
- **2** enterprise connectors (Salesforce, ServiceNow) with real + simulated modes
- **1** dashboard UI

---

## License

MIT

---

Built for enterprises that are ready to stop playing with chatbots and start building autonomous operations.
