using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Constants;

namespace Portfolio.Domain.Entities.WebAppEntities;


[Comment("Reports sent to the db by the iot devices.")]
public partial class IotReport: EntityBase
{
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string DeviceName { get; set; } // todo: Make this Enum 
    
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string Report { get; set; }
}