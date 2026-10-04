using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Entities;

[Comment("List of daily background images by Bing, NASA, etc. They are great")]
public class DailyImage : EntityBase
{
    [Url]
    public required string ImageUrl { get; init; }
    public required string AltText { get; init; }
    public required ImageOfTheDaySource Source { get; init;  }
    public required bool UrlWorks { get; init; }
    public required bool DoIPreferToDisplayThis { get; init; }
}
