using System.ComponentModel.DataAnnotations;

namespace Portfolio.Ui.Models;

public class EmailFormModel
{
    private const int NameMinLength = 3;
    private const int SubjectMinLength = 2;
    private const int MessageMinLength = 15;
    
    [Required(ErrorMessage = "Name is required.")]
    [MinLength(NameMinLength, ErrorMessage = "{0} must be at least {1} characters.")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Subject is required.")]
    [MinLength(SubjectMinLength, ErrorMessage = "{0} must be at least {1} characters.")]
    public string Subject { get; set; } = null!;

    [Required(ErrorMessage = "Message is required.")]
    [MinLength(MessageMinLength, ErrorMessage = "{0} must be at least {1} characters.")]
    public string Message { get; set; } = null!;
}