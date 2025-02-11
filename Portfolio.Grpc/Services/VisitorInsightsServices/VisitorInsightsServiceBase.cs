using Portfolio.Domain.Interfaces.Repositories;

namespace Portfolio.Grpc.Services.VisitorInsightsServices;

public partial class VisitorInsightsService(IRequestLogRepository requestLogRepository) : VisitorInsights.VisitorInsightsBase;