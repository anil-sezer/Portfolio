using Microsoft.AspNetCore.Mvc.RazorPages;
using Portfolio.WebUi.Services;

namespace Portfolio.WebUi.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;
    public string BackgroundImage { get; private set; }
    public string WallpaperFolder { get; private set; }

    public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment webHostEnvironment)
    {
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
        WallpaperFolder = $"{_webHostEnvironment.WebRootPath}/img/wallpapers";
        BackgroundImage = $"{WallpaperFolder}/BiodiverseCostaRica.jpg";
    }

    public void OnGet()
    {
        // todo: If I return like this, then when the page loads I only see the picture. Nice! I need to add this to my notes
        // PhysicalFile(await BackgroundImageFromBingService.GetBackgroundImg($"{_webHostEnvironment.WebRootPath}\\img"), "image/bmp");
        BackgroundImage = BackgroundImageFromBingService.GetBackgroundImg(WallpaperFolder);
        
        Console.WriteLine("Background Img Url: " + BackgroundImage);
    }
}
