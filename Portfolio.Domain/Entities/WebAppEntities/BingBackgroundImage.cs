using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Constants;

namespace Portfolio.Domain.Entities.WebAppEntities;

[Comment("List of daily background images by Bing. They are great")]
public partial class BingDailyBackground: EntityBase
{
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    [Url]
    public string ImageUrl { get; set; }
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public bool UrlWorks { get; set; }
}