using Portfolio.Grpc;

namespace Portfolio.Ui.Models;

public class BackgroundImageModel
{
    public required string Url { get; init; }
    public required string AltText { get; init; }
    public required ImageOfTheDaySource Source { get; init; }
}