using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Governance;

namespace Platform.Governance.Persistence;

public sealed class ScopedAuditLog : IAuditLog
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopedAuditLog(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task RecordAsync(AuditEntry entry, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var inner = scope.ServiceProvider.GetRequiredService<EfCoreAuditLog>();
        await inner.RecordAsync(entry, ct);
    }

    public async Task<IReadOnlyList<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var inner = scope.ServiceProvider.GetRequiredService<EfCoreAuditLog>();
        return await inner.QueryAsync(query, ct);
    }
}