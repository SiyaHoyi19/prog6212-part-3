namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId
    {
        get; set;
    }
    public int EventId
    {
        get; set;
    }
    public string Name { get; set; } = "";
    public string Distance { get; set; } = "";
    public int MinAge
    {
        get; set;
    }
    public Event? Event
    {
        get; set;
    }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
