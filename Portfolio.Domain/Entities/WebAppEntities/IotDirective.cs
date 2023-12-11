using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Constants;

namespace Portfolio.Domain.Entities.WebAppEntities;

[Comment("Commands and parameters for the Iot devices.")]
public partial class IotDirective: EntityBase
{
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string DeviceName { get; set; } // todo: Make this Enum 
    
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string Directives { get; set; }
}