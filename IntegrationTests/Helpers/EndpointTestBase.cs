using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Education.DAL.Context;
using Education.Domain.Entity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Helpers;

public abstract class EndpointTestBase : IAsyncLifetime
{
    protected ApiTestFactory App { get; }
    protected HttpClient Client { get; }
    protected EndpointTestBase(PostgresFixture postgres)
    {
        App = new ApiTestFactory(postgres.ConnectionString);
        Client = App.CreateApiClient();
        using var scope = App.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<EducationDbContext>().Database.GetConnectionString()
            .Should().Be(postgres.ConnectionString);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync()
    {
        Client.Dispose();
        await App.DisposeAsync();
    }

    protected async Task<JsonElement> SendAsync(HttpMethod method, string route, object? body,
        HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var request = new HttpRequestMessage(method, route);
        if (body != null) request.Content = JsonContent.Create(body);
        using var response = await Client.SendAsync(request);
        return await AssertApiAsync(response, expected);
    }

    protected static async Task<JsonElement> AssertApiAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expected, text);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var json = JsonSerializer.Deserialize<JsonElement>(text);
        json.GetProperty("isSuccess").GetBoolean().Should().Be((int)expected < 400);
        json.GetProperty("description").GetString().Should().NotBeNullOrWhiteSpace();
        json.TryGetProperty("data", out _).Should().BeTrue();
        return json;
    }

    protected Task<T> ReadDbAsync<T>(Func<EducationDbContext, Task<T>> query) =>
        App.InScopeAsync(services => query(services.GetRequiredService<EducationDbContext>()));

    protected async Task<TestUserData> RegisterAsync(bool confirm = true)
    {
        var user = TestUserData.New();
        await SendAsync(HttpMethod.Post, "/api/auth/register", user.ToRegisterRequest());
        if (confirm)
            await SendAsync(HttpMethod.Post, "/api/auth/confirm-email", new
            {
                UserId = EmailLinkParser.ExtractQueryParam(App.Email.Last.Body, "userId"),
                Token = EmailLinkParser.ExtractQueryParam(App.Email.Last.Body, "token"),
            });
        return user;
    }

    protected async Task<TestUserData> LoginAsync(string role = "user")
    {
        var user = await RegisterAsync();
        if (role != "user")
        {
            // Setup only: elevated roles have no public assignment endpoint.
            await App.InScopeAsync(async services =>
            {
                var users = services.GetRequiredService<UserManager<UserEntity>>();
                var entity = (await users.FindByEmailAsync(user.Email))!;
                (await users.RemoveFromRoleAsync(entity, "user")).Succeeded.Should().BeTrue();
                (await users.AddToRoleAsync(entity, role)).Succeeded.Should().BeTrue();
                return true;
            });
        }
        await SendAsync(HttpMethod.Post, "/api/auth/login", new { LoginOrEmail = user.Email, user.Password });
        return user;
    }

    protected async Task<Guid> CreateCategoryAsync()
    {
        var name = $"cat_{Guid.NewGuid():N}"[..18];
        await SendAsync(HttpMethod.Post, "/api/category/create", new { Name = name, Description = "Category" }, HttpStatusCode.Created);
        return await ReadDbAsync(db => db.Categories.Where(c => c.Name == name).Select(c => c.Id).SingleAsync());
    }

    protected static MultipartFormDataContent CourseForm(Guid categoryId, Guid? courseId = null, string price = "125.50")
    {
        var form = new MultipartFormDataContent();
        var prefix = courseId.HasValue ? "New" : "";
        form.Add(new StringContent("HTTP course"), prefix + "Title");
        form.Add(new StringContent("Description for HTTP binding tests"), prefix + "Description");
        form.Add(new StringContent(price), prefix + "Price");
        form.Add(new StringContent(categoryId.ToString()), prefix + "CategoryId");
        if (courseId.HasValue) form.Add(new StringContent(courseId.Value.ToString()), "CourseId");
        else
        {
            var image = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1kAAAAASUVORK5CYII="));
            image.Headers.ContentType = new("image/png");
            form.Add(image, "Image", "cover.png");
        }
        return form;
    }

    protected async Task<Guid> CreateCourseAsync(Guid categoryId)
    {
        using var form = CourseForm(categoryId);
        using var response = await Client.PostAsync("/api/course/create", form);
        await AssertApiAsync(response, HttpStatusCode.Created);
        return await ReadDbAsync(db => db.Courses.Where(c => c.CategoryId == categoryId).Select(c => c.Id).SingleAsync());
    }
}
