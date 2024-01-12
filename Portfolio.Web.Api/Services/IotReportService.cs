using System.Text.Json.Nodes;
using Portfolio.Domain.Entities.WebAppEntities;
using Portfolio.Web.Api.Services.Dto;

namespace Portfolio.Web.Api.Services;

public class IotReportService
{
    private readonly WebAppDbContext _dbContext;

    public IotReportService(WebAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task StoreDevicesReportAsync(ReportInput input)
    {
        var jsonString = JsonObject.Create(input.Report).ToJsonString();
        var reportEntry = new IotReport
        {
            DeviceName = input.DeviceName,
            Report = jsonString
        };
        await _dbContext.IotReport.AddAsync(reportEntry);
        await _dbContext.SaveChangesAsync();
    }
}