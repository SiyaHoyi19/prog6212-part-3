using System.ComponentModel.DataAnnotations;
namespace RaceDay.Mvc.Models;
public sealed class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}
