using Grpc.Core;
using Portfolio.Ui.Models;
using Portfolio.Ui.Services;

namespace Portfolio.Ui.Endpoints;

public static class ClusterStatsEndpoints
{
    public const string Route = "/api/cluster/stats";

    public static void MapClusterStatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Route, GetStats)
            .WithName("GetClusterStats");
    }

    internal static async Task<IResult> GetStats(
        ClusterStatsService clusterStatsService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stats = await clusterStatsService.GetFromCacheAsync(cancellationToken);
            var response = new ClusterStatsResponse(
                stats.ActivePodCount,
                stats.ServiceCount,
                stats.DeploymentCount,
                stats.CronJobCount,
                stats.NodeCount);

            return Results.Ok(response);
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499); // Client Closed Request
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            return Results.StatusCode(499);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger("Portfolio.Ui.Endpoints.ClusterStatsEndpoints");
            logger.LogError(ex, "Failed to retrieve cluster stats");
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
