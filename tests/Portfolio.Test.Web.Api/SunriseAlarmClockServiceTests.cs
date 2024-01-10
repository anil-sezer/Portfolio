using FluentAssertions;
using Newtonsoft.Json;
using Portfolio.DataAccess;
using Portfolio.Domain.Entities.WebAppEntities;
using Portfolio.Test.Shared;
using Portfolio.Web.Api.Services;
using Portfolio.Web.Api.Services.Dto;
using Xunit.Abstractions;

namespace Portfolio.Test.Web.Api;

public class SunriseAlarmClockServiceTests : WebApiTestBase
{
    private readonly WebAppDbContext _dbContext;
    private readonly SunriseAlarmClockService _sunriseService;
    private readonly ITestOutputHelper _output;

    public SunriseAlarmClockServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _dbContext = GetDefaultTestDbContext();
        _sunriseService = new SunriseAlarmClockService(_dbContext);
    }

    
    // todo: Edit this code, this is just dirty. Use Theory? Add end of the day tests too!
    [Fact]
    public async Task Should_Get_Contact_Async()
    {
        await CreateTestDataAsync();
        var result = await _sunriseService.SunriseAlarmClockCheckAsync();
        _output.WriteLine(result);

        var directive = JsonConvert.DeserializeObject<SunriseAlarmClockDirective>(result, SunriseAlarmClockService.SerializerSettings);
        directive.BringTheSun.Should().BeTrue();
    }
    
    private async Task CreateTestDataAsync()
    {
        var directive = new SunriseAlarmClockDirective
        {
            AlarmHours = new List<DateTime>
            {
                DateTime.Now,
                DateTime.Now.AddHours(-12)
            },
            BringTheSun = false,
            IsAlarmActive = true,
        };
        await _dbContext.IotDirective.AddAsync(new IotDirective
        {
            DeviceName = SunriseAlarmClockService.SunriseAlarmClock,
            Directives = JsonConvert.SerializeObject(directive, SunriseAlarmClockService.SerializerSettings)
        });
        await _dbContext.SaveChangesAsync();
    }
}
