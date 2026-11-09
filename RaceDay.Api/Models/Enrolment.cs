namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId
    {
        get; set;
    }
    public int UserId
    {
        get; set;
    }
    public int EventId
    {
        get; set;
    }
    public int CategoryId
    {
        get; set;
    }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string Status { get; set; } = "Pending";
    public AppUser? User
    {
        get; set;
    }
    public Event? Event
    {
        get; set;
    }
    public Category? Category
    {
        get; set;
    }
    public Result? Result
    {
        get; set;
    }
}
