using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Mvc.Models;

public sealed class OrganiserDashboardViewModel
{
    public List<OrganiserEventSummaryViewModel> Events { get; set; } = [];
    public int TotalEnrolments => Events.Sum(x => x.EnrolmentCount);
    public int UpcomingEventCount => Events.Count(x => x.Event.Date >= DateOnly.FromDateTime(DateTime.Today));
}
public sealed class OrganiserEventSummaryViewModel
{
    public EventDto Event { get; set; } = default!;
    public int EnrolmentCount
    {
        get; set;
    }
}
public sealed class EventEditViewModel
{
    public string? BannerImageUrl { get; set; }
    public IFormFile? Banner { get; set; }
    public int? EventId
    {
        get; set;
    }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [StringLength(1000)]
    public string? Description
    {
        get; set;
    }
    [Required] public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(1));
    [Required, StringLength(100)] public string Location { get; set; } = "";
    [Range(0.1, 1000)]
    public double Distance
    {
        get; set;
    }
    [Required] public string Type { get; set; } = "Run";
}
public sealed class CategoryEditViewModel
{
    public int EventId
    {
        get; set;
    }
    public int? CategoryId
    {
        get; set;
    }
    [Required, StringLength(50)] public string Name { get; set; } = "";
    [Required, StringLength(50)] public string Distance { get; set; } = "";
    [Range(0, 120)]
    public int MinAge
    {
        get; set;
    }
}
public sealed class OrganiserEnrolmentsViewModel
{
    public EventDto Event { get; set; } = default!;
    public List<OrganiserEnrolmentRowViewModel> Enrolments { get; set; } = [];
}
public sealed class OrganiserEnrolmentRowViewModel
{
    public OrganiserEnrolmentDto Enrolment { get; set; } = default!;
    public string CategoryName { get; set; } = "Category";
    public ResultDto? Result
    {
        get; set;
    }
}
public sealed class OrganiserResultsViewModel
{
    public EventDto Event { get; set; } = default!;
    public List<OrganiserEnrolmentRowViewModel> Enrolments { get; set; } = [];
}
public sealed class ResultEntryViewModel
{
    public int EventId
    {
        get; set;
    }
    public int EnrolmentId
    {
        get; set;
    }
    [Range(0, int.MaxValue)]
    public int? FinishTime
    {
        get; set;
    }
    [StringLength(20)]
    public string? Position
    {
        get; set;
    }
    [Required] public string ResultStatus { get; set; } = "Completed";
}

