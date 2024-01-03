using Microsoft.AspNetCore.Mvc;
using Portfolio.Web.Api.Services;
using Portfolio.Web.Api.Services.Dto;

namespace Portfolio.Web.Api.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class IotController : ControllerBase
{
    private readonly GetIotDirectivesService _directivesService;
    private readonly IotReportService _reportService;
    private readonly SunriseAlarmClockService _sunriseService;

    public IotController(GetIotDirectivesService directivesService, IotReportService reportService, SunriseAlarmClockService sunriseService)
    {
        _directivesService = directivesService;
        _reportService = reportService;
        _sunriseService = sunriseService;
    }
    
    [HttpGet]
    public async Task<ActionResult> GetIotDirectives([FromQuery] string deviceName)
    {
        if (deviceName == "")
            return BadRequest("DeviceName cannot be empty");
        
        var directives = await _directivesService.GetDirectivesByDeviceNameAsync(deviceName);

        if (directives == "")
            return NotFound("Device name not found");

        return Ok(directives);
    }
    
    [HttpPost]
    public async Task<ActionResult> SaveReport([FromBody] ReportInput input)
    {
        await _reportService.SaveReportAsync(input);
        
        return Ok();
    }
    
    [HttpGet]
    public async Task<ActionResult> SunriseAlarmClockCheck()
    {
        var directives = await _sunriseService.SunriseAlarmClockCheckAsync();

        if (directives == "")
            return NotFound("Device not found!");

        return Ok(directives);
    }
}
