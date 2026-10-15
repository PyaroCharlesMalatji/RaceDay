using System.Net;
using System.Net.Http.Json;
using RaceDayApi.Models;

namespace RaceDayApi.Tests;

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ApiTests(ApiFactory factory) { _factory = factory; }

    // Helper: makes a new user, logs them in and returns a client that holds their session
    private async Task<HttpClient> LoggedInClient(string role)
    {
        var client = _factory.CreateClient();   // each client has its own cookie jar
        var email = $"{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("Test", "User", email, "Password1", role));
        await client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, "Password1"));
        return client;
    }

    // Helper: organiser creates an event with one category, returns their ids
    private async Task<(int eventId, int categoryId)> CreateEventWithCategory(HttpClient organiser)
    {
        var evResponse = await organiser.PostAsJsonAsync("/api/events",
            new EventDto("City Run", "Fun run", DateTime.Today.AddDays(30), "Joburg", 10, "run"));
        var ev = await evResponse.Content.ReadFromJsonAsync<Event>();

        var catResponse = await organiser.PostAsJsonAsync($"/api/events/{ev!.Id}/categories", new CategoryDto("10km"));
        var cat = await catResponse.Content.ReadFromJsonAsync<Category>();
        return (ev.Id, cat!.Id);
    }

    // ---------- User registration and login ----------

    [Fact]
    public async Task Register_ValidUser_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("Jane", "Doe", $"{Guid.NewGuid()}@test.com", "Password1", "Participant"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var dto = new RegisterDto("Jane", "Doe", $"{Guid.NewGuid()}@test.com", "Password1", "Participant");
        await client.PostAsJsonAsync("/api/auth/register", dto);
        var res = await client.PostAsJsonAsync("/api/auth/register", dto);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("Jane", "Doe", $"{Guid.NewGuid()}@test.com", "Password1", "Admin"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_CorrectDetails_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto("A", "B", email, "Password1", "Organiser"));
        var res = await client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, "Password1"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto("A", "B", email, "Password1", "Organiser"));
        var res = await client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, "WrongPass"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ---------- Unauthenticated access ----------

    [Fact]
    public async Task GetEvents_NotLoggedIn_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ---------- Event management ----------

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var organiser = await LoggedInClient("Organiser");
        var res = await organiser.PostAsJsonAsync("/api/events",
            new EventDto("Night Ride", "Cycle", DateTime.Today.AddDays(10), "Pretoria", 40, "cycle"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateAndDeleteOwnEvent()
    {
        var organiser = await LoggedInClient("Organiser");
        var (eventId, _) = await CreateEventWithCategory(organiser);

        var update = await organiser.PutAsJsonAsync($"/api/events/{eventId}",
            new EventDto("Renamed", "New", DateTime.Today.AddDays(5), "Sandton", 5, "walk"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await organiser.DeleteAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEvents()
    {
        var participant = await LoggedInClient("Participant");
        var res = await participant.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    // ---------- Role-based access (failure cases) ----------

    [Fact]
    public async Task Participant_CreateEvent_ReturnsForbidden()
    {
        var participant = await LoggedInClient("Participant");
        var res = await participant.PostAsJsonAsync("/api/events",
            new EventDto("Nope", "x", DateTime.Today, "x", 1, "run"));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_Enrol_ReturnsForbidden()
    {
        var organiser = await LoggedInClient("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);
        var res = await organiser.PostAsJsonAsync("/api/enrolments", new EnrolDto(eventId, categoryId));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewEventEnrolments_ReturnsForbidden()
    {
        var organiser = await LoggedInClient("Organiser");
        var (eventId, _) = await CreateEventWithCategory(organiser);
        var participant = await LoggedInClient("Participant");
        var res = await participant.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ---------- Event enrolments ----------

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var organiser = await LoggedInClient("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);

        var participant = await LoggedInClient("Participant");
        var enrol = await participant.PostAsJsonAsync("/api/enrolments", new EnrolDto(eventId, categoryId));
        Assert.Equal(HttpStatusCode.OK, enrol.StatusCode);

        // The participant sees it in their own list
        var mine = await participant.GetFromJsonAsync<List<Enrolment>>("/api/enrolments/mine");
        Assert.Single(mine!);
        Assert.Equal(eventId, mine![0].EventId);
        Assert.Equal(categoryId, mine[0].CategoryId);

        // The organiser sees it in the event's enrolments
        var seen = await organiser.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.OK, seen.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrolTwice_ReturnsConflict()
    {
        var organiser = await LoggedInClient("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);
        var participant = await LoggedInClient("Participant");

        await participant.PostAsJsonAsync("/api/enrolments", new EnrolDto(eventId, categoryId));
        var again = await participant.PostAsJsonAsync("/api/enrolments", new EnrolDto(eventId, categoryId));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }
}
