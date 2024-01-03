using System.ComponentModel.DataAnnotations;
using Portfolio.Domain.Constants;

namespace Portfolio.Web.Api.Services.Dto;

public class ReportInput
{
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string DeviceName { get; set; }
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string Report { get; set; }
}
