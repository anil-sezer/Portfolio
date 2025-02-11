namespace Portfolio.Domain.Interfaces.Repositories.Dtos;

public class EmailDto
{
    public required string Name { get; init; }
    public required string EmailAddress { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
}