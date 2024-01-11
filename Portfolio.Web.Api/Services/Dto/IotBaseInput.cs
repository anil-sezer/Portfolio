using System.ComponentModel.DataAnnotations;
using Portfolio.Domain.Constants;

namespace Portfolio.Web.Api.Services.Dto;

public class IotBaseInput
{
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public string DeviceName { get; set; } // todo: Make this an Enum 
}