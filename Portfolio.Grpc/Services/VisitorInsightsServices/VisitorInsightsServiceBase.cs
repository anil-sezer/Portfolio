using Portfolio.Grpc.BackgroundServices;
using Portfolio.Infrastructure;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService(
    PortfolioDbContext dbContext,
    DatabaseOperationQueue queue,
    ILogger<VisitorInsightsService> logger) : VisitorInsights.VisitorInsightsBase;
