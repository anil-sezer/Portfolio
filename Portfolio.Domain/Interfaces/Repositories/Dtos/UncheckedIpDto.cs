namespace Portfolio.Domain.Interfaces.Repositories.Dtos;

public class UncheckedIpDto
{
    public required int Id { get; init; }
    public required string ClientIp { get; init; }
}
