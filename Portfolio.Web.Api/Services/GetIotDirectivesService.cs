namespace Portfolio.Web.Api.Services;

public class GetIotDirectivesService
{
    private readonly WebAppDbContext _dbContext;

    public GetIotDirectivesService(WebAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GetDirectivesByDeviceNameAsync(string deviceName)
    {
        var devicesDirectives = await _dbContext.IotDirective.SingleOrDefaultAsync(x => x.DeviceName == deviceName);

        if (devicesDirectives == null || devicesDirectives.Directives == "")
            Log.Error("{Device} directives not found!", deviceName);

        return devicesDirectives?.Directives ?? "";
    }
}
