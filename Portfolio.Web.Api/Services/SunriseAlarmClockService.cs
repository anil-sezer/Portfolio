using Newtonsoft.Json;
using Portfolio.Web.Api.Services.Dto;

namespace Portfolio.Web.Api.Services;

public class SunriseAlarmClockService
{
    private readonly WebAppDbContext _dbContext;
    public const string SunriseAlarmClock = nameof(SunriseAlarmClock);

    public static readonly JsonSerializerSettings SerializerSettings = new() { DateFormatString = "HH:mm" };

    public SunriseAlarmClockService(WebAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    // todo: Edit this code, this is just dirty
    public async Task<string> SunriseAlarmClockCheckAsync()
    {
        var devicesDirectives = await _dbContext.IotDirective.AsNoTracking().SingleAsync(x => x.DeviceName == SunriseAlarmClock);

        if (devicesDirectives == null)
        {
            Log.Fatal("{Sunrise} not found! Is db active?", SunriseAlarmClock);
            throw new Exception(); // todo: Need custom exceptions
        }
        
        var directive = JsonConvert.DeserializeObject<SunriseAlarmClockDirective>(devicesDirectives.Directives, SerializerSettings);

        foreach (var t in directive!.AlarmHours)
        {
            var minutesDifference = Math.Abs((t.TimeOfDay - DateTime.Now.TimeOfDay).TotalMinutes);

            if (minutesDifference <= 30)
                directive.BringTheSun = true; // Alarm begins!
        }

        if (directive.BringTheSun)
            Log.Information("Time to shine!");

        return JsonConvert.SerializeObject(directive);
    }
}
