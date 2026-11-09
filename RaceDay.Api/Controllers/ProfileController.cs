using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Api.Services;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[Route("api/profile/me")]
public class ProfileController(RaceDayDbContext db) : ApiControllerBase(db)
{
    [HttpGet]
    [SwaggerOperation(Summary = "Get your profile", Description = "Returns the signed-in user's own profile, including their role and saved profile-image URL.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get()
    {
        if (RequireLogin() is IActionResult error)
        {
            return error;
        }

        var user = await Db.Users
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.UserId == CurrentUserId);

        return user is null
            ? NotFound()
            : Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.DateOfBirth,
                user.ProfileImageUrl,
                Role = user.Role!.Name
            });
    }

    [HttpPut]
    [SwaggerOperation(Summary = "Update your profile", Description = "Updates the signed-in user's name, email, phone number, and date of birth. Email must not already belong to another user.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(UpdateProfileRequest request)
    {
        if (RequireLogin() is IActionResult error)
        {
            return error;
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await Db.Users.FindAsync(CurrentUserId);
        if (user is null)
        {
            return NotFound();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await Db.Users.AnyAsync(item => item.Email == email && item.UserId != user.UserId))
        {
            return Conflict(new
            {
                message = "Email already in use."
            });
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.PhoneNumber = request.PhoneNumber.Trim();
        user.DateOfBirth = request.DateOfBirth;

        await Db.SaveChangesAsync();

        return Ok(new
        {
            user.UserId,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.DateOfBirth
        });
    }

    [HttpPost("image"), Consumes("multipart/form-data"), RequestSizeLimit(5 * 1024 * 1024)]
    [SwaggerOperation(Summary = "Upload your profile image", Description = "Send one JPG, PNG, or WebP image, up to 5 MB, as multipart form field 'file'. The API stores it in Azure Blob Storage and saves its URL on the signed-in user's profile.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(IFormFile? file,
        [FromServices] BlobStorageService blobStorage, CancellationToken cancellationToken)
    {
        if (RequireLogin() is IActionResult error) return error;
        if (!BlobStorageService.IsValidImage(file))
            return BadRequest(new { message = "Choose a JPG, PNG, or WebP image smaller than 5 MB." });

        var user = await Db.Users.FindAsync(new object[] { CurrentUserId!.Value }, cancellationToken);
        if (user is null) return NotFound();

        user.ProfileImageUrl = await blobStorage.UploadAsync(file!, "profile-images", cancellationToken);
        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { imageUrl = user.ProfileImageUrl, profileImageUrl = user.ProfileImageUrl });
    }

    [HttpGet("image")]
    [SwaggerOperation(Summary = "Get your profile image", Description = "Returns the signed-in user's stored profile image.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Image([FromServices] BlobStorageService blobStorage,
        CancellationToken cancellationToken)
    {
        if (RequireLogin() is IActionResult error) return error;
        var user = await Db.Users.FindAsync(new object[] { CurrentUserId!.Value }, cancellationToken);
        if (user?.ProfileImageUrl is null) return NotFound();
        var image = await blobStorage.DownloadAsync(user.ProfileImageUrl, cancellationToken);
        return image is null ? NotFound() : File(image.Content, image.ContentType);
    }
}
