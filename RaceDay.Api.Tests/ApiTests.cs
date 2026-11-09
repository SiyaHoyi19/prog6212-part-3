using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Contracts;
using Xunit;

public class ApiFactory : WebApplicationFactory<Program>
{
    // Keep one database name for this factory so data persists between requests.
    private readonly string _databaseName = "RaceDayTests_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                service =>
                    service.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpointWithoutSessionReturns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/profile/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationAndLoginWorkAndDuplicateEmailConflicts()
    {
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var request = new RegisterRequest(
            "Test Organiser",
            "org@example.com",
            "StrongPassword123!",
            "0712345678",
            new DateOnly(1990, 1, 1),
            "Organiser",
            "orgtest");

        var registration = await client.PostAsJsonAsync(
            "/api/authentication/register", request);

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var duplicate = await client.PostAsJsonAsync(
            "/api/authentication/register", request);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var login = await client.PostAsJsonAsync(
            "/api/authentication/login",
            new LoginRequest("org@example.com", "StrongPassword123!"));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task InvalidCredentialsAreRejected()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/authentication/login",
            new LoginRequest("missing@example.com", "wrongpassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ParticipantCannotCreateEventAndOrganiserCan()
    {
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        await Register(client, "participant@example.com", "Participant", "parttest");
        await Login(client, "participant@example.com");

        var eventRequest = new CreateEventRequest(
            "Test race", "Test", new DateOnly(2027, 5, 1),
            "Gqeberha", 5, "Run");

        var participantResponse =
            await client.PostAsJsonAsync("/api/events", eventRequest);

        Assert.Equal(HttpStatusCode.Forbidden, participantResponse.StatusCode);

        await client.PostAsync("/api/authentication/logout", content: null);

        await Register(client, "organiser@example.com", "Organiser", "orgtest2");
        await Login(client, "organiser@example.com");

        var organiserResponse =
            await client.PostAsJsonAsync("/api/events", eventRequest);

        Assert.Equal(HttpStatusCode.Created, organiserResponse.StatusCode);
    }

    [Fact]
    public async Task ParticipantCanEnrolAndDuplicateIsRejected()
    {
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        await Register(client, "organiser2@example.com", "Organiser", "orgtest3");
        await Login(client, "organiser2@example.com");

        var eventResponse = await client.PostAsJsonAsync(
            "/api/events",
            new CreateEventRequest(
                "Test race", "Test", new DateOnly(2027, 5, 1),
                "Gqeberha", 5, "Run"));

        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);

        var eventJson = await eventResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(eventJson.TryGetProperty("eventId", out var eventIdValue),
            "The event response did not contain eventId.");
        var eventId = eventIdValue.GetInt32();

        var categoryResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new CreateCategoryRequest("5km", "5km", 16));

        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);

        var categoryJson =
            await categoryResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(categoryJson.TryGetProperty("categoryId", out var categoryIdValue),
            "The category response did not contain categoryId.");
        var categoryId = categoryIdValue.GetInt32();

        await client.PostAsync("/api/authentication/logout", content: null);

        await Register(client, "participant2@example.com", "Participant", "parttest2");
        await Login(client, "participant2@example.com");

        var enrolmentRequest = new CreateEnrolmentRequest(eventId, categoryId);

        var enrolmentResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments", enrolmentRequest);

        Assert.Equal(HttpStatusCode.Created, enrolmentResponse.StatusCode);

        var duplicateResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments", enrolmentRequest);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task ProfileCanBeUpdatedAndDuplicateEmailIsRejected()
    {
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"profile-{suffix}@example.com";
        await Register(client, email, "Participant", $"profile-{suffix}");
        await Login(client, email);

        var update = await client.PutAsJsonAsync("/api/profile/me",
            new UpdateProfileRequest("Updated Participant", email, "0711111111", new DateOnly(2000, 1, 1)));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/profile/me");
        Assert.Equal("Updated Participant", profile.GetProperty("fullName").GetString());

        var otherEmail = $"other-{suffix}@example.com";
        await Register(client, otherEmail, "Participant", $"other-{suffix}");
        await client.PostAsync("/api/authentication/logout", content: null);
        await Login(client, email);

        var duplicate = await client.PutAsJsonAsync("/api/profile/me",
            new UpdateProfileRequest("Updated Participant", otherEmail, "0711111111", new DateOnly(2000, 1, 1)));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task OrganiserCanUpdateAndDeleteOwnEventAndCategory()
    {
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"manage-{suffix}@example.com";
        await Register(client, email, "Organiser", $"manage-{suffix}");
        await Login(client, email);

        var createdEvent = await client.PostAsJsonAsync("/api/events",
            new CreateEventRequest("Original event", "Original description", new DateOnly(2027, 5, 1), "Gqeberha", 5, "Run"));
        Assert.Equal(HttpStatusCode.Created, createdEvent.StatusCode);
        var eventJson = await createdEvent.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = eventJson.GetProperty("eventId").GetInt32();

        var createdCategory = await client.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CreateCategoryRequest("5 km", "5 km", 16));
        Assert.Equal(HttpStatusCode.Created, createdCategory.StatusCode);
        var categoryJson = await createdCategory.Content.ReadFromJsonAsync<JsonElement>();
        var categoryId = categoryJson.GetProperty("categoryId").GetInt32();

        var categoryUpdate = await client.PutAsJsonAsync($"/api/categories/{categoryId}",
            new CreateCategoryRequest("10 km", "10 km", 18));
        Assert.Equal(HttpStatusCode.OK, categoryUpdate.StatusCode);

        var eventUpdate = await client.PutAsJsonAsync($"/api/events/{eventId}",
            new UpdateEventRequest("Updated event", "Updated description", new DateOnly(2027, 6, 1), "Gqeberha", 10, "Walk"));
        Assert.Equal(HttpStatusCode.OK, eventUpdate.StatusCode);

        var categoryDelete = await client.DeleteAsync($"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.OK, categoryDelete.StatusCode);
        var eventDelete = await client.DeleteAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.OK, eventDelete.StatusCode);
    }

    [Fact]
    public async Task OwnershipFailuresReturn403AndDependentRecordsPreventDeletion()
    {
        var suffix = Guid.NewGuid().ToString("N");
        using var owner = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var participant = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var otherOrganiser = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var ownerEmail = $"owner-{suffix}@example.com";
        await Register(owner, ownerEmail, "Organiser", $"owner-{suffix}");
        await Login(owner, ownerEmail);
        var eventResponse = await owner.PostAsJsonAsync("/api/events",
            new CreateEventRequest("Owned event", "Ownership test", new DateOnly(2027, 7, 1), "Gqeberha", 5, "Run"));
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        var eventJson = await eventResponse.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = eventJson.GetProperty("eventId").GetInt32();
        var categoryResponse = await owner.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CreateCategoryRequest("5 km", "5 km", 0));
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var categoryJson = await categoryResponse.Content.ReadFromJsonAsync<JsonElement>();
        var categoryId = categoryJson.GetProperty("categoryId").GetInt32();

        var participantEmail = $"entrant-{suffix}@example.com";
        await Register(participant, participantEmail, "Participant", $"entrant-{suffix}");
        await Login(participant, participantEmail);
        var enrolmentResponse = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments",
            new CreateEnrolmentRequest(eventId, categoryId));
        Assert.Equal(HttpStatusCode.Created, enrolmentResponse.StatusCode);

        var otherEmail = $"otherorganiser-{suffix}@example.com";
        await Register(otherOrganiser, otherEmail, "Organiser", $"otherorg-{suffix}");
        await Login(otherOrganiser, otherEmail);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherOrganiser.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherOrganiser.GetAsync($"/api/events/{eventId}/results")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherOrganiser.PutAsJsonAsync($"/api/categories/{categoryId}",
                new CreateCategoryRequest("Changed", "5 km", 0))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherOrganiser.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherOrganiser.PutAsJsonAsync($"/api/events/{eventId}",
                new UpdateEventRequest("Changed", null, new DateOnly(2027, 7, 1), "Gqeberha", 5, "Run"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherOrganiser.DeleteAsync($"/api/events/{eventId}")).StatusCode);

        Assert.Equal(HttpStatusCode.Conflict,
            (await owner.DeleteAsync($"/api/events/{eventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await owner.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
    }

    [Fact]
    public async Task OrganiserRecordsResultAndParticipantCanViewIt()
    {
        var suffix = Guid.NewGuid().ToString("N");
        using var organiser = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var participant = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var organiserEmail = $"results-{suffix}@example.com";
        await Register(organiser, organiserEmail, "Organiser", $"results-{suffix}");
        await Login(organiser, organiserEmail);
        var eventResponse = await organiser.PostAsJsonAsync("/api/events",
            new CreateEventRequest("Results event", "Test", new DateOnly(2027, 8, 1), "Gqeberha", 5, "Run"));
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        var eventId = (await eventResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("eventId").GetInt32();
        var categoryResponse = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CreateCategoryRequest("5 km", "5 km", 0));
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var categoryId = (await categoryResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("categoryId").GetInt32();

        var participantEmail = $"runner-{suffix}@example.com";
        await Register(participant, participantEmail, "Participant", $"runner-{suffix}");
        await Login(participant, participantEmail);
        var enrolmentResponse = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments",
            new CreateEnrolmentRequest(eventId, categoryId));
        Assert.Equal(HttpStatusCode.Created, enrolmentResponse.StatusCode);
        var enrolmentId = (await enrolmentResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("enrolmentId").GetInt32();

        var resultRequest = new CreateResultRequest(enrolmentId, 42, "1st", "Completed");
        var resultResponse = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", resultRequest);
        Assert.Equal(HttpStatusCode.Created, resultResponse.StatusCode);
        var duplicate = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", resultRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var participantResults = await participant.GetAsync("/api/user/results");
        Assert.Equal(HttpStatusCode.OK, participantResults.StatusCode);
        var resultList = await participantResults.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, resultList.GetArrayLength());
    }

    private static async Task Register(
        HttpClient client, string email, string role, string username)
    {
        var response = await client.PostAsJsonAsync(
            "/api/authentication/register",
            new RegisterRequest(
                "Test User",
                email,
                "StrongPassword123!",
                "0712345678",
                new DateOnly(1995, 1, 1),
                role,
                username));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/authentication/login",
            new LoginRequest(email, "StrongPassword123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
