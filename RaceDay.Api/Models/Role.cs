namespace RaceDay.Api.Models;

public class Role
{
    public int RoleId
    {
        get; set;
    }
    public string Name { get; set; } = "";
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
