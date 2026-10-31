using Portfolio.Domain.Enums;
using Portfolio.Domain.Extensions;

namespace Portfolio.Domain.Entities;

public class DailyImage : EntityBase
{
    public const int MaxImageUrlLength = 2048;
    public const int MaxAltTextLength = 1000;

    public required string ImageUrl { get; init; }

    private readonly string _altText = string.Empty;
    public required string AltText
    {
        get => _altText;
        init => _altText = value.Truncate(MaxAltTextLength);
    }

    public required ImageOfTheDaySource Source { get; init; }
    public required bool UrlWorks { get; init; }
    public required bool DoIPreferToDisplayThis { get; init; }
}
