using Microsoft.AspNetCore.Mvc.RazorPages;
using Portfolio.WebUi.Services;
using Serilog;

namespace Portfolio.WebUi.Pages;

public class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    public string BackgroundImage { get; private set; }
    public string WallpaperFolder { get; private set; }

    public IndexModel(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
        WallpaperFolder = $"{_webHostEnvironment.WebRootPath}/img/wallpapers";
        BackgroundImage = $"{WallpaperFolder}/BiodiverseCostaRica.jpg";
    }

    public void OnGet()
    {
        // todo: If I return like this, then when the page loads I only see the picture. Nice! I need to add this to my notes
        // PhysicalFile(await BackgroundImageFromBingService.GetBackgroundImg($"{_webHostEnvironment.WebRootPath}\\img"), "image/bmp");
        BackgroundImage = BackgroundImageFromBingService.GetBackgroundImg(WallpaperFolder);
        
        Log.Information("Background Img Url: {BackgroundImage}", BackgroundImage);
    }
}
