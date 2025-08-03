using Serilog;
using Portfolio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Interfaces.Repositories;
using Portfolio.Domain.Interfaces.Repositories.Dtos;

namespace Portfolio.Infrastructure.Repositories;

public class RequestLogRepository(PortfolioDbContext dbContext): BaseRepository<RequestLog>(dbContext), IRequestLogRepository
{
    public async Task<List<UncheckedIpDto>> GetRowsOfUncheckedIpsAsync()
    {
        return await dbContext.RequestLogs
            .Where(x => x.ClientIp != "" &&
                        x.City == "" &&
                        x.Country == "")
            .Select(x => new UncheckedIpDto
            {
                Id = x.Id,
                ClientIp = x.ClientIp
            })
            .ToListAsync();
    }

    public async Task UpdateRowWithCityAndCountryInfoAsync(int rowId, string city, string country)
    {
        var rowToUpdate = await GetByIdAsync(rowId);
        
        if (rowToUpdate == null)
        {
            Log.Error("Request log with id {Id} not found, what happened?", rowId);
            return;
        }
        
        rowToUpdate.UpdateLocation(city, country);

        await dbContext.SaveChangesAsync();
    }
}
