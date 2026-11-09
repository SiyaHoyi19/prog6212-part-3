namespace RaceDay.Api.Models;

public class AppUser
{
    public int UserId
    {
        get; set;
    }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public DateOnly DateOfBirth
    {
        get; set;
    }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? ProfileImageUrl { get; set; }
    public int RoleId
    {
        get; set;
    }
    public Role? Role
    {
        get; set;
    }
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
