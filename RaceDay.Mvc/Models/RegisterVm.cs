using System.ComponentModel.DataAnnotations;
namespace RaceDay.Mvc.Models;
public sealed class RegisterViewModel
{
    [Required, MaxLength(50)] public string FullName { get; set; } = "";
    [Required, EmailAddress, MaxLength(50)] public string Email { get; set; } = "";
    [Required, MinLength(8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, MaxLength(15)] public string PhoneNumber { get; set; } = "";
    [DataType(DataType.Date)] public DateOnly DateOfBirth { get; set; }
    [Required] public string Role { get; set; } = "Participant";
    [MaxLength(50)] public string? Username { get; set; }
}
