using Portfolio.Domain.Entities.WebAppEntities;

namespace Portfolio.Web.Api.Services;

public class IotReportService
{
    private readonly WebAppDbContext _dbContext;

    public IotReportService(WebAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveReportAsync(string deviceName, string report)
    {
        var reportEntry = new IotReport
        {
            DeviceName = deviceName,
            Report = report
        };

        await _dbContext.IotReport.AddAsync(reportEntry);
        await _dbContext.SaveChangesAsync();
    }
}