using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Portfolio.Domain.Constants;

namespace Portfolio.Web.Api.Services.Dto;

public class ReportInput: IotBaseInput
{
    [Required(ErrorMessage = Etcetera.DataAnnotation_RequiredErrorMsg)]
    public JsonElement Report { get; set; }
}
