namespace RaceDay.Api.Models;

public class Event
{
    public int EventId
    {
        get; set;
    }
    public int OrganiserId
    {
        get; set;
    }
    public string Name { get; set; } = "";
    public string? Description
    {
        get; set;
    }
    public DateOnly Date
    {
        get; set;
    }
    public string Location { get; set; } = "";
    public double Distance
    {
        get; set;
    }
    public string Type { get; set; } = "Run";
    public string? BannerImageUrl { get; set; }
    public AppUser? Organiser
    {
        get; set;
    }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

