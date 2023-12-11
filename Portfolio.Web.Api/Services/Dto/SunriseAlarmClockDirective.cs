namespace Portfolio.Web.Api.Services.Dto;

public class SunriseAlarmClockDirective
{
    public List<DateTime> AlarmHours { get; set; }
    public bool IsAlarmActive { get; set; }
    public bool BringTheSun { get; set; }
}