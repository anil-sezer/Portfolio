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
    public async Task<ActionResult> GetAllIotDirectives()
    {
        var directives = await _directivesService.GetAllDirectivesAsync();

        return Ok(directives);
    }
    
    [HttpGet]
    public async Task<ActionResult> GetIotDirectives([FromQuery] IotBaseInput input)
    {
        var directives = await _directivesService.GetDirectivesByDeviceNameAsync(input);

        return Ok(directives);
    }
    
    [HttpPost]
    public async Task<ActionResult> StoreDevicesReport([FromBody] ReportInput input)
    {
        await _reportService.StoreDevicesReportAsync(input);
        
        return Ok();
    }
    
    [HttpPut]
    public async Task<ActionResult> UpdateDeviceDirectives(UpdateDeviceDirectivesInput input)
    {
        var directives = await _directivesService.UpdateDeviceDirectivesAsync(input);

        return Ok(directives);
    }
    
    [HttpGet]
    public async Task<ActionResult> SunriseAlarmClockCheck()
    {
        var directives = await _sunriseService.SunriseAlarmClockCheckAsync();

        return Ok(directives);
    }
}
