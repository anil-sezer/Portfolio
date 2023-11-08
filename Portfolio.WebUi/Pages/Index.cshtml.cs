using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portfolio.DataAccess;
using Portfolio.Domain.Entities.WebAppEntities;
using Portfolio.WebUi.Services;
using Serilog;

namespace Portfolio.WebUi.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    public string Name { get; set; }

    [BindProperty]
    public string Email { get; set; }
    
    [BindProperty]
    public string Subject { get; set; }

    [BindProperty]
    public string Message { get; set; }
    
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
            Country = "",
            City = ""
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

    // public async Task OnPostAsync()
    // {
    //     try
    //     {
    //         var email = new Email
    //         {
    //             Name = Name,
    //             Subject = Subject,
    //             EmailAddress = Email,
    //             Message = Message
    //         };
    //
    //         await _dbContext.Email.AddAsync(email);
    //         await _dbContext.SaveChangesAsync();
    //     }
    //     catch (Exception e)
    //     {
    //         Log.Warning(e, "Mail service had an exception. Probably caused by a invalid email address. Address provided: {Email}", Email);
    //     }
    // }
    
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var email = new Email
            {
                Name = Name,
                Subject = Subject,
                EmailAddress = Email,
                Message = Message
            };

            await _dbContext.Email.AddAsync(email);
            await _dbContext.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }
        catch (Exception e)
        {
            Log.Warning(e, "Mail service had an exception. Probably caused by an invalid email address. Address provided: {Email}", Email);
            return new JsonResult(new { success = false });
        }
    }
}
