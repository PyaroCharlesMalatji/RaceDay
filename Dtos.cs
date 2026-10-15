using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models;

public record RegisterDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string Role);

public record LoginDto([Required] string Email, [Required] string Password);

public record ProfileDto([Required] string FirstName, [Required] string LastName);

public record EventDto(
    [Required] string Name, string Description, DateTime Date,
    [Required] string Location, double DistanceKm, [Required] string EventType);

public record CategoryDto([Required] string Name);

public record EnrolDto(int EventId, int CategoryId);

public record ResultDto(int EnrolmentId, string FinishTime, int Position); // FinishTime like "00:45:30"