using Platform.Core.Agents;

namespace Platform.Orchestration.Registry;

public sealed class AgentRegistry
{
    private readonly Dictionary<string, IAgent> _agents = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        _agents[agent.Identity.AgentId] = agent;
    }

    public IAgent Resolve(string agentId)
    {
        if (!_agents.TryGetValue(agentId, out var agent))
            throw new InvalidOperationException(string.Format("Agent '{0}' is not registered.", agentId));
        return agent;
    }

    public bool TryResolve(string agentId, out IAgent? agent) => _agents.TryGetValue(agentId, out agent);

    public IReadOnlyCollection<AgentIdentity> ListAll() => _agents.Values.Select(a => a.Identity).ToList();
}
