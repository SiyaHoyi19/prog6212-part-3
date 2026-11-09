using System.ComponentModel.DataAnnotations;
public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
public class RegisterVm
{
    [Required] public string FirstName { get; set; } = string.Empty;
    [Required] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = "Participant";
}
