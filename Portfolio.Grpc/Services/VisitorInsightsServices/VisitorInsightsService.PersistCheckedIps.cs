using System.Text.Json;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService
{
    public override async Task<Empty> PersistCheckedIps(PersistCheckedIpsRequest request, ServerCallContext context)
    {
        Log.Information("Request to log: {Log}",JsonSerializer.Serialize(request));
        
        Log.Information("✅ Received {IpCount} ips for handling", request.CheckedIps.Count);

        await RemoveUnwantedRowsAsync(request);

        await UpdateLocationInfoAsync(request);

        return new Empty();
    }
    
    private async Task RemoveUnwantedRowsAsync(PersistCheckedIpsRequest request)
    {
        var unwantedRowsIds = request.CheckedIps
            .Where(x => x.Operation == DbOperationForThisRow.Delete)
            .Select(x => x.EntityId)
            .ToList();
        
        await requestLogRepository.RemoveRowsAsync(unwantedRowsIds);
    }
    
    private async Task UpdateLocationInfoAsync(PersistCheckedIpsRequest request)
    {
        var ipsToUpdate = request.CheckedIps.Where(x => x.Operation == DbOperationForThisRow.Update).ToList();
        foreach (var ip in ipsToUpdate)
            await requestLogRepository.UpdateRowWithCityAndCountryInfoAsync(ip.EntityId, ip.City, ip.Country);
    }
}
