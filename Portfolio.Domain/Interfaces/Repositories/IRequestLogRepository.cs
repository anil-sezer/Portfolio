using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces.Repositories.Dtos;

namespace Portfolio.Domain.Interfaces.Repositories;

public interface IRequestLogRepository: IRepository<RequestLog>
{
    Task<List<UncheckedIpDto>> GetRowsOfUncheckedIpsAsync();
    Task UpdateRowWithCityAndCountryInfoAsync(int rowId, string city, string country);
}
