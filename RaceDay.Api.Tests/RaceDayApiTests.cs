using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Api.Tests;

public class RaceDayApiTests
{
    private static object RegisterBody(string email, string role) => new
    {
        firstName = "Test", lastName = "Runner", email, password = "StrongPass!2026",
        phoneNumber = "0821234567", role
    };

    private static async Task RegisterAndLogin(HttpClient client, string email, string role)
    {
        var register = await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, role));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "StrongPass!2026" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Register_CreatesUser_AndRejectsDuplicateEmail()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient(new() { HandleCookies = true });

        var first = await client.PostAsJsonAsync("/api/auth/register", RegisterBody("runner1@example.com", "Participant"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var json = await first.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);

        var duplicate = await client.PostAsJsonAsync("/api/auth/register", RegisterBody("runner1@example.com", "Participant"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Register_RejectsInvalidRole()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", RegisterBody("badrole@example.com", "Administrator"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_RejectsWrongPassword_AndAcceptsCorrectPassword()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient(new() { HandleCookies = true });
        var register = await client.PostAsJsonAsync("/api/auth/register", RegisterBody("login@example.com", "Participant"));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var badLogin = await client.PostAsJsonAsync("/api/auth/login", new { email = "login@example.com", password = "WrongPassword!" });
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);

        var goodLogin = await client.PostAsJsonAsync("/api/auth/login", new { email = "login@example.com", password = "StrongPass!2026" });
        Assert.Equal(HttpStatusCode.OK, goodLogin.StatusCode);
        var profile = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Fact]
    public async Task ProtectedProfile_RejectsUnauthenticatedRequest()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient(new() { HandleCookies = true });
        await RegisterAndLogin(client, "participant@example.com", "Participant");

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventTypeId = 1, eventName = "Test Run", description = "Test event",
            eventDate = DateTime.UtcNow.AddDays(10), location = "Cape Town", distanceKm = 10.0m
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_AndParticipantCanEnrol()
    {
        using var factory = new ApiFactory();
        using var organiser = factory.CreateClient(new() { HandleCookies = true });
        using var participant = factory.CreateClient(new() { HandleCookies = true });
        await RegisterAndLogin(organiser, "organiser@example.com", "Organiser");
        await RegisterAndLogin(participant, "participant2@example.com", "Participant");

        var createEvent = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventTypeId = 1, eventName = "Cape Town Test 10K", description = "A test race",
            eventDate = DateTime.UtcNow.AddDays(20), location = "Green Point, Cape Town", distanceKm = 10.0m
        });
        Assert.Equal(HttpStatusCode.Created, createEvent.StatusCode);
        var createdEvent = await createEvent.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(createdEvent);

        var createCategory = await organiser.PostAsJsonAsync($"/api/events/{createdEvent!.EventId}/categories", new
        {
            categoryName = "Open", categoryType = "Age", minAge = 18, maxAge = 99
        });
        Assert.Equal(HttpStatusCode.Created, createCategory.StatusCode);
        var category = await createCategory.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(category);

        var enrol = await participant.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = createdEvent.EventId, categoryId = category!.CategoryId
        });
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);

        var mine = await participant.GetAsync("/api/enrolments/mine");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var organiserView = await organiser.GetAsync($"/api/events/{createdEvent.EventId}/enrolments");
        Assert.Equal(HttpStatusCode.OK, organiserView.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewAnotherParticipantsEnrolment()
    {
        using var factory = new ApiFactory();
        using var organiser = factory.CreateClient(new() { HandleCookies = true });
        using var participant1 = factory.CreateClient(new() { HandleCookies = true });
        using var participant2 = factory.CreateClient(new() { HandleCookies = true });
        await RegisterAndLogin(organiser, "owner2@example.com", "Organiser");
        await RegisterAndLogin(participant1, "runner2@example.com", "Participant");
        await RegisterAndLogin(participant2, "runner3@example.com", "Participant");

        var ev = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventTypeId = 2, eventName = "Community Walk", description = "Walk test",
            eventDate = DateTime.UtcNow.AddDays(15), location = "Soweto", distanceKm = 5.0m
        });
        var eventData = await ev.Content.ReadFromJsonAsync<EventResponse>();
        var cat = await organiser.PostAsJsonAsync($"/api/events/{eventData!.EventId}/categories", new
        {
            categoryName = "5km", categoryType = "Distance", minDistanceKm = 5.0m, maxDistanceKm = 5.0m
        });
        var catData = await cat.Content.ReadFromJsonAsync<CategoryResponse>();
        var enrol = await participant1.PostAsJsonAsync("/api/enrolments", new { eventId = eventData.EventId, categoryId = catData!.CategoryId });
        var enrolData = await enrol.Content.ReadFromJsonAsync<EnrolmentResponse>();

        var forbidden = await participant2.GetAsync($"/api/enrolments/{enrolData!.EnrolmentId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInSameEvent()
    {
        using var factory = new ApiFactory();
        using var organiser = factory.CreateClient(new() { HandleCookies = true });
        using var participant = factory.CreateClient(new() { HandleCookies = true });
        await RegisterAndLogin(organiser, "owner3@example.com", "Organiser");
        await RegisterAndLogin(participant, "runner4@example.com", "Participant");

        var ev = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventTypeId = 3, eventName = "Cycle Test", description = "Cycle event",
            eventDate = DateTime.UtcNow.AddDays(30), location = "Durban", distanceKm = 40.0m
        });
        var eventData = await ev.Content.ReadFromJsonAsync<EventResponse>();
        var cat = await organiser.PostAsJsonAsync($"/api/events/{eventData!.EventId}/categories", new
        {
            categoryName = "Open 40km", categoryType = "Distance", minDistanceKm = 40.0m, maxDistanceKm = 40.0m
        });
        var catData = await cat.Content.ReadFromJsonAsync<CategoryResponse>();
        var body = new { eventId = eventData.EventId, categoryId = catData!.CategoryId };

        var first = await participant.PostAsJsonAsync("/api/enrolments", body);
        var second = await participant.PostAsJsonAsync("/api/enrolments", body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
    [Fact]
    public async Task Organiser_CanRecordResult_AndParticipantCanViewOwnResult()
    {
        using var factory = new ApiFactory();
        using var organiser = factory.CreateClient(new() { HandleCookies = true });
        using var participant = factory.CreateClient(new() { HandleCookies = true });
        await RegisterAndLogin(organiser, "results-owner@example.com", "Organiser");
        await RegisterAndLogin(participant, "results-runner@example.com", "Participant");

        var eventResponse = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventTypeId = 1, eventName = "Completed Test Race", description = "Race used for result test",
            eventDate = DateTime.UtcNow.AddDays(5), location = "Cape Town", distanceKm = 10.0m
        });
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        var eventData = await eventResponse.Content.ReadFromJsonAsync<EventResponse>();

        var categoryResponse = await organiser.PostAsJsonAsync($"/api/events/{eventData!.EventId}/categories", new
        {
            categoryName = "Senior", categoryType = "Age", minAge = 20, maxAge = 99
        });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        var enrolResponse = await participant.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = eventData.EventId, categoryId = category!.CategoryId
        });
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponse>();

        var updateEvent = await organiser.PutAsJsonAsync($"/api/events/{eventData.EventId}", new
        {
            eventTypeId = 1, eventName = "Completed Test Race", description = "Race used for result test",
            eventDate = DateTime.UtcNow.AddDays(-1), location = "Cape Town", distanceKm = 10.0m,
            eventStatus = "Completed"
        });
        Assert.Equal(HttpStatusCode.OK, updateEvent.StatusCode);

        var resultResponse = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment!.EnrolmentId}/result", new
        {
            finishTime = "00:48:32", finishingPosition = 47, totalFinishers = 312
        });
        Assert.Equal(HttpStatusCode.Created, resultResponse.StatusCode);

        var myResults = await participant.GetAsync("/api/results/mine");
        Assert.Equal(HttpStatusCode.OK, myResults.StatusCode);
        var resultRows = await myResults.Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.NotNull(resultRows);
        Assert.Single(resultRows);
        Assert.Equal(47, resultRows[0].FinishingPosition);
    }

}
