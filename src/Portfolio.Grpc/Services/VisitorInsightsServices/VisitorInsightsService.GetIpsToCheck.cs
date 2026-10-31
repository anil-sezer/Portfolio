using Microsoft.EntityFrameworkCore;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService
{
    public const int MaxBatchSize = 1000;

    public override async Task<GetIpsToCheckResponse> GetIpsToCheck(Empty request, ServerCallContext context)
    {

        var ips = await dbContext.RequestLogs
            .AsNoTracking()
            .Where(x => x.ClientIp != "" && x.City == "" && x.Country == "")
            .OrderBy(x => x.Id)
            .Take(MaxBatchSize)
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
        
        logger.LogInformation("✅ Sent {IpCount} ips for checking", response.Ips.Count);
        return response;
    }
}