using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService
{
    public override async Task<GetIpsToCheckResponse> GetIpsToCheck(Empty request, ServerCallContext context)
    {
        Log.Information("Request to log: {Log}", JsonSerializer.Serialize(request));

        var ips = await dbContext.RequestLogs
            .Where(x => x.ClientIp != "" && x.City == "" && x.Country == "")
            .Select(x => new IpCheckDto
            {
                EntityId = x.Id,
                IpAddress = x.ClientIp,
                City = "",
                Country = "",
                Operation = DbOperationForThisRow.Unprocessed
            })
            .ToListAsync(context.CancellationToken);

        var response = new GetIpsToCheckResponse();
        response.Ips.AddRange(ips);
        
        Log.Information("✅ Sent {IpCount} ips for checking", response.Ips.Count);
        return response;
    }
}