using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Constants;

namespace Portfolio.Domain.Entities.WebAppEntities;


// todo: DB sütunlarına belli değerlerde olma zorunluluğu vs şeyleri bu katmanda ekleyebilir misin? Oku!
// https://docs.microsoft.com/en-us/ef/core/modeling/indexes?tabs=data-annotations
// [Table("Request")]
[Comment("Accesses to the website")]
public partial class Request: EntityBase
{
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string UserAgent { get; set; }
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string AcceptLanguage { get; set; }
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string ClientIp { get; set; }
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string DeviceType { get; set; }
}