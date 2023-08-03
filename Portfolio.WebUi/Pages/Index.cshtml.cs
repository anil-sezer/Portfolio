using Microsoft.AspNetCore.Mvc.RazorPages;
using Portfolio.WebUi.Services;

namespace Portfolio.WebUi.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment webHostEnvironment)
    {
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
    }

    public async void OnGetAsync()
    {
        // _webHostEnvironment.WebRootPath
        // C:\Users\msnan\source\repos\anilsezar\Portfolio\Portfolio.WebUi\wwwroot\img
        await BackgroundImageFromBingService.GetBackgroundImg(_webHostEnvironment.WebRootPath + "\\img");
    }
}
