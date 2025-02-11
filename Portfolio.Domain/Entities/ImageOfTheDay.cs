using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Entities;

[Comment("List of daily background images by Bing, NASA, etc. They are great")]
public class ImageOfTheDay : EntityBase
{
    // public ImageOfTheDay(string imageUrl, string altText, ImageOfTheDaySource source, bool urlWorks, bool doIPreferToDisplayThis)
    // {
    //     ImageUrl = imageUrl;
    //     AltText = altText;
    //     Source = source;
    //     UrlWorks = urlWorks;
    //     DoIPreferToDisplayThis = doIPreferToDisplayThis;
    // }

    [Url]
    public required string ImageUrl { get; init; }
    public required string AltText { get; init; }
    public required ImageOfTheDaySource Source { get; init;  }
    public required bool UrlWorks { get; init; }
    public required bool DoIPreferToDisplayThis { get; init; }
}
