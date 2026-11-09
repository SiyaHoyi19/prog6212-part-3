namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId
    {
        get; set;
    }
    public int EnrolmentId
    {
        get; set;
    }
    public int? FinishTime
    {
        get; set;
    }
    public string? Position
    {
        get; set;
    }
    public string ResultStatus { get; set; } = "Completed";
    public Enrolment? Enrolment
    {
        get; set;
    }
}
