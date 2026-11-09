using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Mvc.Models;

public sealed class ParticipantEventsViewModel
{
    public List<EventDto> Events { get; set; } = [];
    public string? Type
    {
        get; set;
    }
    public string? Location
    {
        get; set;
    }
    public string? Search
    {
        get; set;
    }
}

public sealed class ParticipantEventDetailsViewModel
{
    public EventDto Event { get; set; } = default!;
    public List<CategoryDto> Categories { get; set; } = [];
}

public sealed class ParticipantEnrolmentRowViewModel
{
    public EnrolmentDto Enrolment { get; set; } = default!;
    public EventDto? Event
    {
        get; set;
    }
    public string CategoryName { get; set; } = "Category";
}

public sealed class ParticipantResultRowViewModel
{
    public ResultDto Result { get; set; } = default!;
    public EventDto? Event
    {
        get; set;
    }
    public string CategoryName { get; set; } = "Category";
}

public sealed class ParticipantProfileViewModel
{
    public string? ProfileImageUrl { get; set; }
    [Required, StringLength(50)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(50)] public string Email { get; set; } = "";
    [Required, StringLength(15)] public string PhoneNumber { get; set; } = "";
    [DataType(DataType.Date)]
    public DateOnly DateOfBirth
    {
        get; set;
    }
    public string Role { get; set; } = "Participant";
}
