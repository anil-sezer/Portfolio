namespace Portfolio.Ui.Models;

public sealed record ClusterStatsResponse(
    int ActivePodCount,
    int ServiceCount,
    int DeploymentCount,
    int CronJobCount,
    int NodeCount);
