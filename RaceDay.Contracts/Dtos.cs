using System.ComponentModel.DataAnnotations;
namespace RaceDay.Contracts;

public record RegisterRequest([Required, MaxLength(50)] string FullName, [Required, EmailAddress, MaxLength(50)] string Email, [Required, MinLength(8)] string Password, [Required, MaxLength(15)] string PhoneNumber, DateOnly DateOfBirth, [Required] string Role, [MaxLength(50)] string? Username = null);
public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public record AuthResponse(int UserId, string FullName, string Email, string Role, string Message);
public record UpdateProfileRequest([Required, MaxLength(50)] string FullName, [Required, EmailAddress, MaxLength(50)] string Email, [Required, MaxLength(15)] string PhoneNumber, DateOnly DateOfBirth);
public record CreateEventRequest([Required, MaxLength(100)] string Name, [MaxLength(1000)] string? Description, DateOnly Date, string Location, double Distance, string Type);
public record UpdateEventRequest([Required, MaxLength(100)] string Name, [MaxLength(1000)] string? Description, DateOnly Date, string Location, double Distance, string Type);
public record EventDto(int EventId, int OrganiserId, string Name, string? Description, DateOnly Date, string Location, double Distance, string Type, string? BannerImageUrl = null);
public record CreateCategoryRequest([Required, MaxLength(50)] string Name, [Required, MaxLength(50)] string Distance, int MinAge);
public record CategoryDto(int CategoryId, int EventId, string Name, string Distance, int MinAge);
public record CreateEnrolmentRequest(int EventId, int CategoryId);
public record UpdateEnrolmentStatusRequest([Required] string Status);
public record EnrolmentDto(int EnrolmentId, int UserId, int EventId, int CategoryId, DateOnly Date, string Status);
public record CreateResultRequest(int EnrolmentId, int? FinishTime, string? Position, [Required] string ResultStatus);
public record ResultDto(int ResultId, int EnrolmentId, int? FinishTime, string? Position, string ResultStatus);
public record OrganiserEnrolmentDto(int EnrolmentId, int UserId, string ParticipantName, int EventId, int CategoryId, string CategoryName, DateOnly Date, string Status);
