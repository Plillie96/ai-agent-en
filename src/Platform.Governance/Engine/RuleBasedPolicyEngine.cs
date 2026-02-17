using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Platform.Core.Governance;

namespace Platform.Governance.Engine;

public sealed class RuleBasedPolicyEngine : IPolicyEngine
{
    private readonly List<GovernancePolicy> _policies = [];
    private readonly ILogger<RuleBasedPolicyEngine> _logger;

    public RuleBasedPolicyEngine(ILogger<RuleBasedPolicyEngine> logger)
    {
        _logger = logger;
    }

    public void AddPolicy(GovernancePolicy policy) => _policies.Add(policy);

    public Task<PolicyDecision> EvaluateAsync(PolicyEvaluationContext context, CancellationToken ct = default)
    {
        var applicablePolicies = _policies
            .Where(p => p.IsActive)
            .Where(p => p.Scope == PolicyScope.Global
                || (p.Scope == PolicyScope.Department && string.Equals(p.Department, context.Department, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(p => p.Scope)
            .ToList();

        foreach (var policy in applicablePolicies)
        {
            foreach (var rule in policy.Rules.OrderByDescending(r => r.Severity))
            {
                if (EvaluateRule(rule, context))
                {
                    _logger.LogInformation("Policy {PolicyId} rule {RuleId} matched: {Description}", policy.PolicyId, rule.RuleId, rule.Description);

                    return Task.FromResult(new PolicyDecision
                    {
                        Action = rule.Action,
                        PolicyId = policy.PolicyId,
                        RuleId = rule.RuleId,
                        Reason = rule.Description
                    });
                }
            }
        }

        return Task.FromResult(new PolicyDecision
        {
            Action = PolicyAction.Allow,
            PolicyId = "default",
            RuleId = "default-allow",
            Reason = "No matching policy rules; defaulting to allow"
        });
    }

    private static bool EvaluateRule(PolicyRule rule, PolicyEvaluationContext context)
    {
        var expression = rule.ConditionExpression;

        // Pattern: "action:pattern" - matches action name
        if (expression.StartsWith("action:", StringComparison.OrdinalIgnoreCase))
        {
            var pattern = expression[7..];
            return Regex.IsMatch(context.Action, pattern, RegexOptions.IgnoreCase);
        }

        // Pattern: "agent:agentId" - matches specific agent
        if (expression.StartsWith("agent:", StringComparison.OrdinalIgnoreCase))
        {
            var agentId = expression[6..];
            return string.Equals(context.AgentId, agentId, StringComparison.OrdinalIgnoreCase);
        }

        // Pattern: "param:key:op:value" - checks parameter values
        if (expression.StartsWith("param:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = expression.Split(':', 4);
            if (parts.Length == 4 && context.Parameters.TryGetValue(parts[1], out var paramValue))
            {
                return EvaluateComparison(paramValue, parts[2], parts[3]);
            }
            return false;
        }

        // Pattern: "always" - always matches
        if (string.Equals(expression, "always", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool EvaluateComparison(object value, string op, string expected)
    {
        var strValue = value?.ToString() ?? "";
        return op.ToLowerInvariant() switch
        {
            "eq" => string.Equals(strValue, expected, StringComparison.OrdinalIgnoreCase),
            "neq" => !string.Equals(strValue, expected, StringComparison.OrdinalIgnoreCase),
            "contains" => strValue.Contains(expected, StringComparison.OrdinalIgnoreCase),
            "gt" => decimal.TryParse(strValue, out var a) && decimal.TryParse(expected, out var b) && a > b,
            "lt" => decimal.TryParse(strValue, out var c) && decimal.TryParse(expected, out var d) && c < d,
            _ => false
        };
    }
}