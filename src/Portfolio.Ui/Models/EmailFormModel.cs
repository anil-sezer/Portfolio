using System.ComponentModel.DataAnnotations;
using Portfolio.Domain.Entities;

namespace Portfolio.Ui.Models;

public class EmailFormModel
{
    private const int NameMinLength = 3;
    private const int SubjectMinLength = 2;
    private const int MessageMinLength = 15;
    
    [Required(ErrorMessage = "Name is required.")]
    [MinLength(NameMinLength, ErrorMessage = "{0} must be at least {1} characters.")]
    [MaxLength(NotificationToAdmin.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(NotificationToAdmin.MaxEmailAddressLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Subject is required.")]
    [MinLength(SubjectMinLength, ErrorMessage = "{0} must be at least {1} characters.")]
    [MaxLength(NotificationToAdmin.MaxSubjectLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string Subject { get; set; } = null!;

    [Required(ErrorMessage = "Message is required.")]
    [MinLength(MessageMinLength, ErrorMessage = "{0} must be at least {1} characters.")]
    [MaxLength(NotificationToAdmin.MaxMessageLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string Message { get; set; } = null!;
}