using Platform.Core.Governance;
using Platform.Core.Workflows;
using Platform.Governance.Engine;
using Platform.Orchestration.Registry;
using Platform.Runtime.Agents;
using Platform.Runtime.Agents.Finance;
using Platform.Runtime.Agents.HR;
using Platform.Runtime.Agents.IT;
using Platform.Runtime.Agents.Legal;
using Platform.Runtime.Agents.Procurement;

namespace Platform.Runtime.Setup;

public static class PlatformBootstrap
{
    public static AgentRegistry ConfigureAgents()
    {
        var registry = new AgentRegistry();

        // Sales
        registry.Register(new DealAnalysisAgent());
        registry.Register(new PricingAgent());
        registry.Register(new DocumentAgent());
        registry.Register(new FollowUpAgent());

        // Finance
        registry.Register(new RevenueRecognitionAgent());
        registry.Register(new RiskFlaggingAgent());

        // Legal
        registry.Register(new ContractClassificationAgent());
        registry.Register(new ContractRedlineAgent());

        // IT / Security
        registry.Register(new SecurityResponseAgent());
        registry.Register(new TicketingAgent());

        // Procurement
        registry.Register(new VendorNegotiationAgent());
        registry.Register(new ProcurementCommsAgent());

        // HR
        registry.Register(new AttritionDetectionAgent());
        registry.Register(new RetentionActionAgent());

        return registry;
    }

    public static WorkflowRegistry ConfigureWorkflows()
    {
        var registry = new WorkflowRegistry();

        // === SALES: Stalled Deal Recovery ===
        registry.Register(new WorkflowDefinition
        {
            WorkflowId = "sales-stalled-deal-recovery",
            Name = "Stalled Deal Recovery",
            Department = "Sales",
            Description = "Detects stalled deals, analyzes objections, generates revised pricing, sends docs, books follow-up",
            TriggerEventType = "sales.opportunity.stalled",
            Steps =
            [
                new StepDefinition
                {
                    StepId = "analyze-deal", AgentId = "sales-deal-analysis", Name = "Analyze Stalled Deal",
                    InputMappings = new Dictionary<string, string> { ["DealId"] = "dealId", ["DaysStalled"] = "daysStalled", ["Amount"] = "amount" },
                    Timeout = TimeSpan.FromMinutes(5)
                },
                new StepDefinition
                {
                    StepId = "revise-pricing", AgentId = "sales-pricing", Name = "Generate Revised Pricing",
                    InputMappings = new Dictionary<string, string> { ["Amount"] = "Amount" },
                    Timeout = TimeSpan.FromMinutes(2)
                },
                new StepDefinition
                {
                    StepId = "send-docs", AgentId = "sales-documents", Name = "Send Security Documentation",
                    Timeout = TimeSpan.FromMinutes(2)
                },
                new StepDefinition
                {
                    StepId = "book-followup", AgentId = "sales-followup", Name = "Book Follow-Up and Notify",
                    InputMappings = new Dictionary<string, string> { ["RevisedAmount"] = "RevisedAmount" },
                    Timeout = TimeSpan.FromMinutes(2)
                }
            ]
        });

        // === FINANCE: Month-End Reconciliation ===
        registry.Register(new WorkflowDefinition
        {
            WorkflowId = "finance-month-end-reconciliation",
            Name = "Month-End Revenue Reconciliation",
            Department = "Finance",
            Description = "Detects revenue recognition inconsistencies, cross-checks contracts, flags risky entries, prepares variance report",
            TriggerEventType = "finance.month-end.triggered",
            Steps =
            [
                new StepDefinition
                {
                    StepId = "detect-inconsistencies", AgentId = "finance-revenue-recognition", Name = "Detect Revenue Inconsistencies",
                    Timeout = TimeSpan.FromMinutes(10)
                },
                new StepDefinition
                {
                    StepId = "flag-risk", AgentId = "finance-risk-flagging", Name = "Flag Risky Entries and Generate Variance Report",
                    RequiresApproval = true,
                    Timeout = TimeSpan.FromMinutes(5)
                }
            ]
        });

        // === LEGAL: Contract Review ===
        registry.Register(new WorkflowDefinition
        {
            WorkflowId = "legal-contract-review",
            Name = "Automated Contract Review",
            Department = "Legal",
            Description = "Classifies contract, extracts clauses, compares against playbook, redlines non-standard terms, routes high-risk to counsel",
            TriggerEventType = "legal.contract.received",
            Steps =
            [
                new StepDefinition
                {
                    StepId = "classify-contract", AgentId = "legal-contract-classification", Name = "Classify and Extract Contract Clauses",
                    InputMappings = new Dictionary<string, string> { ["ContractType"] = "contractType", ["ContractId"] = "contractId" },
                    Timeout = TimeSpan.FromMinutes(5)
                },
                new StepDefinition
                {
                    StepId = "redline-contract", AgentId = "legal-contract-redline", Name = "Redline Non-Standard Terms",
                    InputMappings = new Dictionary<string, string> { ["OverallRiskScore"] = "OverallRiskScore" },
                    Timeout = TimeSpan.FromMinutes(5)
                }
            ]
        });

        // === IT: Security Incident Response ===
        registry.Register(new WorkflowDefinition
        {
            WorkflowId = "it-security-incident-response",
            Name = "Security Incident Response",
            Department = "IT",
            Description = "Correlates identity + behavior logs, scores risk, restricts access, opens ticket, generates forensic report",
            TriggerEventType = "it.security.anomaly-detected",
            Steps =
            [
                new StepDefinition
                {
                    StepId = "analyze-threat", AgentId = "it-security-response", Name = "Correlate and Analyze Security Threat",
                    InputMappings = new Dictionary<string, string> { ["UserId"] = "userId", ["AnomalyScore"] = "anomalyScore" },
                    Timeout = TimeSpan.FromMinutes(3)
                },
                new StepDefinition
                {
                    StepId = "create-ticket", AgentId = "it-ticketing", Name = "Create Incident Ticket and Notify CISO",
                    Timeout = TimeSpan.FromMinutes(2)
                }
            ]
        });

        // === PROCUREMENT: Vendor Price Negotiation ===
        registry.Register(new WorkflowDefinition
        {
            WorkflowId = "procurement-vendor-negotiation",
            Name = "Vendor Price Negotiation",
            Department = "Procurement",
            Description = "Compares benchmarks, checks escalation clauses, generates counterproposal, sends negotiation email",
            TriggerEventType = "procurement.vendor.price-increase",
            Steps =
            [
                new StepDefinition
                {
                    StepId = "analyze-pricing", AgentId = "procurement-vendor-negotiation", Name = "Analyze Pricing and Generate Counter",
                    InputMappings = new Dictionary<string, string> { ["VendorId"] = "vendorId", ["PriceIncrease"] = "priceIncrease" },
                    Timeout = TimeSpan.FromMinutes(5)
                },
                new StepDefinition
                {
                    StepId = "send-counter", AgentId = "procurement-comms", Name = "Send Counterproposal to Vendor",
                    InputMappings = new Dictionary<string, string> { ["CounterProposal"] = "CounterProposal" },
                    Timeout = TimeSpan.FromMinutes(2)
                }
            ]
        });

        // === HR: Attrition Prevention ===
        registry.Register(new WorkflowDefinition
        {
            WorkflowId = "hr-attrition-prevention",
            Name = "Proactive Attrition Prevention",
            Department = "HR",
            Description = "Detects attrition risk, analyzes sentiment and comp, recommends retention packages, schedules manager check-ins",
            TriggerEventType = "hr.attrition.risk-spike",
            Steps =
            [
                new StepDefinition
                {
                    StepId = "detect-attrition", AgentId = "hr-attrition-detection", Name = "Detect Attrition Risk and Analyze Factors",
                    InputMappings = new Dictionary<string, string> { ["TeamId"] = "teamId" },
                    Timeout = TimeSpan.FromMinutes(5)
                },
                new StepDefinition
                {
                    StepId = "retention-action", AgentId = "hr-retention-action", Name = "Generate Retention Packages and Schedule Check-ins",
                    InputMappings = new Dictionary<string, string> { ["AtRiskEmployees"] = "AtRiskEmployees" },
                    RequiresApproval = true,
                    Timeout = TimeSpan.FromMinutes(3)
                }
            ]
        });

        return registry;
    }

    public static RuleBasedPolicyEngine ConfigurePolicies(Microsoft.Extensions.Logging.ILogger<RuleBasedPolicyEngine> logger)
    {
        var engine = new RuleBasedPolicyEngine(logger);

        // Global: audit everything
        engine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "global-audit",
            Name = "Global Audit Trail",
            Scope = PolicyScope.Global,
            Rules = [new PolicyRule { RuleId = "audit-all", Description = "Audit all agent actions", ConditionExpression = "always", Action = PolicyAction.Audit, Severity = PolicySeverity.Low }]
        });

        // Block automated financial transfers
        engine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "finance-transfer-block",
            Name = "Financial Transfer Guard",
            Scope = PolicyScope.Global,
            Rules = [new PolicyRule { RuleId = "block-transfers", Description = "Block automated financial transfers without human approval", ConditionExpression = "action:.*transfer.*", Action = PolicyAction.Deny, Severity = PolicySeverity.Critical }]
        });

        // Sales: require approval for discounts > 20%
        engine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "sales-discount-limit",
            Name = "Sales Discount Threshold",
            Department = "Sales",
            Scope = PolicyScope.Department,
            Rules = [new PolicyRule { RuleId = "discount-over-20pct", Description = "Require approval for discounts exceeding 20%", ConditionExpression = "param:ProposedDiscount:gt:0.20", Action = PolicyAction.RequireApproval, Severity = PolicySeverity.High }]
        });

        // Finance: require approval for booking reclassification
        engine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "finance-reclass-approval",
            Name = "Revenue Reclassification Approval",
            Department = "Finance",
            Scope = PolicyScope.Department,
            Rules = [new PolicyRule { RuleId = "reclass-approval", Description = "Require human review for revenue reclassification", ConditionExpression = "action:.*Flag Risky.*", Action = PolicyAction.RequireApproval, Severity = PolicySeverity.High }]
        });

        // Legal: require counsel for high-risk contracts
        engine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "legal-high-risk",
            Name = "High-Risk Contract Escalation",
            Department = "Legal",
            Scope = PolicyScope.Department,
            Rules = [new PolicyRule { RuleId = "high-risk-contract", Description = "Escalate high-risk contracts to senior counsel", ConditionExpression = "param:OverallRiskScore:gt:7.0", Action = PolicyAction.RequireApproval, Severity = PolicySeverity.High }]
        });

        // IT: alert on critical security actions
        engine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "it-security-alert",
            Name = "Security Action Alert",
            Department = "IT",
            Scope = PolicyScope.Department,
            Rules = [new PolicyRule { RuleId = "security-alert", Description = "Alert on all security incident response actions", ConditionExpression = "agent:it-security-response", Action = PolicyAction.Alert, Severity = PolicySeverity.Critical }]
        });

        // Procurement: require approval for counter-offers > $50K savings
        engine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "procurement-high-value",
            Name = "High-Value Negotiation Approval",
            Department = "Procurement",
            Scope = PolicyScope.Department,
            Rules = [new PolicyRule { RuleId = "high-value-counter", Description = "Require approval for counter-offers with >$50K impact", ConditionExpression = "param:SavingsIfAccepted:gt:50000", Action = PolicyAction.RequireApproval, Severity = PolicySeverity.High }]
        });

        // HR: require approval for retention packages
        engine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "hr-retention-approval",
            Name = "Retention Package Approval",
            Department = "HR",
            Scope = PolicyScope.Department,
            Rules = [new PolicyRule { RuleId = "retention-approval", Description = "Require VP approval for retention packages", ConditionExpression = "action:.*Retention.*", Action = PolicyAction.RequireApproval, Severity = PolicySeverity.High }]
        });

        return engine;
    }
}