using Portfolio.Infrastructure;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService(PortfolioDbContext dbContext, ILogger<VisitorInsightsService> logger) : VisitorInsights.VisitorInsightsBase;