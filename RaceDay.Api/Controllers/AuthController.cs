using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[Route("api/authentication")]
public sealed class AuthController(RaceDayDbContext db) : ApiControllerBase(db)
{
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Register a new user",
        Description = "Creates an Organiser or Participant account with a salted PBKDF2 password hash.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var roleName = request.Role.Trim();

        if (roleName.Equals("organiser", StringComparison.OrdinalIgnoreCase))
            roleName = "Organiser";
        else if (roleName.Equals("participant", StringComparison.OrdinalIgnoreCase))
            roleName = "Participant";
        else
            return BadRequest(new
            {
                message = "Role must be Organiser or Participant."
            });

        var email = request.Email.Trim().ToLowerInvariant();
        var username = string.IsNullOrWhiteSpace(request.Username)
            ? email.Split('@')[0]
            : request.Username.Trim();

        if (await Db.Users.AnyAsync(user => user.Email == email, cancellationToken))
            return Conflict(new
            {
                message = "Email already registered."
            });

        if (await Db.Users.AnyAsync(user => user.Username == username, cancellationToken))
            return Conflict(new
            {
                message = "Username already in use."
            });

        var role = await Db.Roles
            .SingleOrDefaultAsync(item => item.Name == roleName, cancellationToken);

        if (role is null)
        {
            role = new Role { Name = roleName };
            Db.Roles.Add(role);
            await Db.SaveChangesAsync(cancellationToken);
        }

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            DateOfBirth = request.DateOfBirth,
            Username = username,
            PasswordHash = PasswordService.Hash(request.Password),
            RoleId = role.RoleId
        };

        Db.Users.Add(user);

        try
        {
            await Db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Email or username is already registered."
            });
        }

        return Created("/api/profile/me", new
        {
            userId = user.UserId,
            fullName = user.FullName,
            email = user.Email,
            role = role.Name
        });
    }

    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Log in",
        Description = "Checks credentials and stores user ID, role, and name in the server session.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await Db.Users
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.Email == email, cancellationToken);

        if (user?.Role is null ||
            !PasswordService.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role.Name);
        HttpContext.Session.SetString("FullName", user.FullName);

        return Ok(new AuthResponse(
            user.UserId,
            user.FullName,
            user.Email,
            user.Role.Name,
            "Login successful. Keep the session cookie for subsequent requests."));
    }

    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "Log out",
        Description = "Clears the current server session.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new
        {
            message = "Logged out."
        });
    }
}
