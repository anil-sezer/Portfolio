using System.ComponentModel.DataAnnotations;
using Portfolio.Domain.Constants;

namespace Portfolio.Web.Api.Services.Dto;

public class UpdateDeviceDirectivesInput: IotBaseInput
{
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string Directives { get; set; }
}