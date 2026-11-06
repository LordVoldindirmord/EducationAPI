using System.Net;
using System.Globalization;
using IntegrationTests.Helpers;

namespace IntegrationTests.Tests.Http;

[Collection(AuthIntegrationCollection.Name)]
public class CategoryEndpointTests(PostgresFixture postgres) : EndpointTestBase(postgres)
{
    [Fact]
    public async Task Create_AcceptsJson_Returns201()
    {
        await LoginAsync("admin");
        await CreateCategoryAsync();
    }

    [Fact]
    public async Task Update_AcceptsPutJson_Returns200()
    {
        await LoginAsync("moderator");
        var id = await CreateCategoryAsync();
        await SendAsync(HttpMethod.Put, "/api/category/update", new { CategoryId = id, NewName = "Updated", NewDescription = "Changed" });
    }

    [Fact]
    public async Task Delete_AcceptsDeleteJson_Returns200()
    {
        await LoginAsync("admin");
        var id = await CreateCategoryAsync();
        await SendAsync(HttpMethod.Delete, "/api/category/delete", new { CategoryId = id });
    }
}

[Collection(AuthIntegrationCollection.Name)]
public class CourseEndpointTests(PostgresFixture postgres) : EndpointTestBase(postgres)
{
    [Theory]
    [InlineData("ru-RU")]
    [InlineData("en-US")]
    public async Task Create_BindsMultipartWithImage_Returns201(string culture)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        App.Server.PreserveExecutionContext = true;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            await LoginAsync("admin");
            await CreateCourseAsync(await CreateCategoryAsync());
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public async Task Create_CommaDecimal_Returns400InsteadOfChangingPrice()
    {
        await LoginAsync("admin");
        var categoryId = await CreateCategoryAsync();
        using var form = CourseForm(categoryId, price: "125,50");
        using var response = await Client.PostAsync("/api/course/create", form);
        await AssertApiAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_BindsPutMultipart_Returns200()
    {
        await LoginAsync("admin");
        var categoryId = await CreateCategoryAsync();
        var courseId = await CreateCourseAsync(categoryId);
        using var form = CourseForm(categoryId, courseId);
        using var response = await Client.PutAsync("/api/course/update", form);
        await AssertApiAsync(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task Publish_BindsPutJson_Returns200()
    {
        await LoginAsync("admin");
        var id = await CreateCourseAsync(await CreateCategoryAsync());
        await SendAsync(HttpMethod.Put, "/api/course/publish", new { CourseId = id, IsPublished = true });
    }

    [Fact]
    public async Task Delete_BindsDeleteJson_Returns200()
    {
        await LoginAsync("admin");
        var id = await CreateCourseAsync(await CreateCategoryAsync());
        await SendAsync(HttpMethod.Delete, "/api/course/delete", new { CourseId = id });
    }
}
