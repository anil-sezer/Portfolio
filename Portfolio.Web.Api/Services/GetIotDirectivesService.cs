using Portfolio.Domain.Entities.WebAppEntities;
using Portfolio.Web.Api.Services.Dto;

namespace Portfolio.Web.Api.Services;

public class GetIotDirectivesService // todo-anil-beforeMerge: Rename
{
    private readonly WebAppDbContext _dbContext;

    public GetIotDirectivesService(WebAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GetDirectivesByDeviceNameAsync(IotBaseInput input)
    {
        var devicesDirectives = await GetDeviceDirectivesAsync(input.DeviceName);

        return devicesDirectives.Directives;
    }
    
    public async Task<List<IotDirective>> GetAllDirectivesAsync()
    {
        var allDevicesDirectives = await _dbContext.IotDirective.AsNoTracking().ToListAsync();

        return allDevicesDirectives;
    }
    
    public async Task<IotDirective> UpdateDeviceDirectivesAsync(UpdateDeviceDirectivesInput input)
    {
        var devicesDirectives = await GetDeviceDirectivesAsync(input.DeviceName);

        devicesDirectives.Directives = input.Directives;
        await _dbContext.SaveChangesAsync();

        return devicesDirectives;
    }

    private async Task<IotDirective> GetDeviceDirectivesAsync(string deviceName)
    {
        var devicesDirectives = await _dbContext.IotDirective.SingleOrDefaultAsync(x => x.DeviceName == deviceName);

        if (devicesDirectives != null && devicesDirectives.Directives != "")
            return devicesDirectives;
        
        // todo-anil: needs custom exception flow. I need to edit the response maybe? I dont wanna send 500 for this!
        Log.Error("{Device} directives not found!", deviceName);
        throw new Exception($"{deviceName} directives not found!");
    }
}
