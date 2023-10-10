using DeviceDetectorNET;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portfolio.DataAccess;
using Portfolio.Domain.Constants;
using Portfolio.Domain.Entities.WebAppEntities;
using Portfolio.WebUi.Services;
using Serilog;

namespace Portfolio.WebUi.Pages;

public class IndexModel : PageModel
{
    private readonly BackgroundImageFromBingService _backgroundImageFromBing;
    public string BackgroundImage { get; private set; }
    private readonly WebAppDbContext _dbContext;

    public IndexModel(WebAppDbContext dbContext, BackgroundImageFromBingService backgroundImageFromBing)
    {
        _dbContext = dbContext;
        _backgroundImageFromBing = backgroundImageFromBing;
    }

    public async Task OnGetAsync()
    {
        // todo: If I return like this, then when the page loads I only see the picture. Nice! I need to add this to my notes
        // PhysicalFile(await BackgroundImageFromBingService.GetBackgroundImg($"{_webHostEnvironment.WebRootPath}\\img"), "image/bmp");

        BackgroundImage = await _backgroundImageFromBing.GetBackgroundImg();

        LogRequestAsync();
        
        Log.Information("Background Img Url: {BackgroundImage}", BackgroundImage);
    }

    private async Task LogRequestAsync()
    {
        await _dbContext.Request.AddAsync(new Request
        {
            AcceptLanguage = Request.Headers.AcceptLanguage.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            ClientIp = TryToGetIp(),
            DeviceType = Enum.GetName(TryToGetDeviceType())
        });
        await _dbContext.SaveChangesAsync();
    }

    private string TryToGetIp()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        return string.IsNullOrEmpty(forwardedFor) 
            ? HttpContext.Connection.RemoteIpAddress.ToString()
            : forwardedFor.Split(',').FirstOrDefault();
    }
    
    private Devices TryToGetDeviceType()
    {
        var userAgent = Request.Headers.UserAgent;
        var headers = Request.Headers.ToDictionary(a => a.Key, a => a.Value.ToArray().FirstOrDefault());
        var clientHints = ClientHints.Factory(headers);

        var dd = new DeviceDetector(userAgent, clientHints);

        return dd.IsDesktop() ? Devices.Desktop :
            dd.IsBrowser() ? Devices.Browser :
            dd.IsMobile() ? Devices.Mobile :
            dd.IsTablet() ? Devices.Tablet :
            dd.IsBot() ? Devices.Bot :
            Devices.Unknown;
    }
}
