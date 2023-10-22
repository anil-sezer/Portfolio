using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Constants;

namespace Portfolio.Domain.Entities.WebAppEntities;


// todo: DB sütunlarına belli değerlerde olma zorunluluğu vs şeyleri bu katmanda ekleyebilir misin? Oku!
// https://docs.microsoft.com/en-us/ef/core/modeling/indexes?tabs=data-annotations
[Comment("Emails that have been sent to the admin via the website")]
public partial class Email: EntityBase
{
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string Name { get; set; }
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    [EmailAddress]
    public string EmailAddress { get; set; }
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string Subject { get; set; }
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string Message { get; set; }
}