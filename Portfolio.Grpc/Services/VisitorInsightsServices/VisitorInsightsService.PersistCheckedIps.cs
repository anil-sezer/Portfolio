using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService
{
    public override async Task<Empty> PersistCheckedIps(PersistCheckedIpsRequest request, ServerCallContext context)
    {
        Log.Information("Request to log: {Log}", JsonSerializer.Serialize(request));
        
        Log.Information("✅ Received {IpCount} ips for handling", request.CheckedIps.Count);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(context.CancellationToken);

            await RemoveUnwantedRowsAsync(request, context.CancellationToken);
            await UpdateLocationInfoAsync(request, context.CancellationToken);
            await dbContext.SaveChangesAsync(context.CancellationToken);

            await transaction.CommitAsync(context.CancellationToken);
        });

        return new Empty();
    }
    
    private async Task RemoveUnwantedRowsAsync(PersistCheckedIpsRequest request, CancellationToken cancellationToken = default)
    {
        var unwantedRowsIds = request.CheckedIps
            .Where(x => x.Operation == DbOperationForThisRow.Delete)
            .Select(x => x.EntityId)
            .ToList();

        if (unwantedRowsIds.Count == 0) return;

        await dbContext.RequestLogs
            .Where(x => unwantedRowsIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
    
    private async Task UpdateLocationInfoAsync(PersistCheckedIpsRequest request, CancellationToken cancellationToken = default)
    {
        var ipsToUpdate = request.CheckedIps.Where(x => x.Operation == DbOperationForThisRow.Update).ToList();
        if (ipsToUpdate.Count == 0) return;

        var ids = ipsToUpdate.Select(x => x.EntityId).ToList();
        var rows = await dbContext.RequestLogs
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var rowLookup = rows.ToDictionary(x => x.Id);
        foreach (var ip in ipsToUpdate)
        {
            if (rowLookup.TryGetValue(ip.EntityId, out var row))
            {
                row.UpdateLocation(ip.City, ip.Country);
            }
            else
            {
                Log.Warning("Request log with id {Id} not found to update city country information", ip.EntityId);
            }
        }
    }
}
