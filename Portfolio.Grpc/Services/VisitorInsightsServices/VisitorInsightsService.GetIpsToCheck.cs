using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService
{
    public override async Task<GetIpsToCheckResponse> GetIpsToCheck(Empty r, ServerCallContext context)
    {
        Log.Information("Request to log: {Log}",JsonSerializer.Serialize(r));

        var ips = await requestLogRepository.GetRowsOfUncheckedIpsAsync();

        var response = new GetIpsToCheckResponse();
        foreach (var ip in ips)
        {
            response.Ips.Add(new IpCheckDto
            {
                EntityId = ip.Id,
                IpAddress = ip.ClientIp,
                City = "",
                Country = "",
                Operation = DbOperationForThisRow.Unprocessed
            });
        }
        
        Log.Information("✅ Sent {IpCount} ips for checking", response.Ips.Count);
        return response;
    }
}