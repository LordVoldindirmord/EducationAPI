using EducationAPI.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEducationServices(builder.Configuration);
builder.Services.AddIdentity();
builder.Services.AddControllersWithJson();
builder.Services.AddFrontendCors();

var app = builder.Build();

#region Development (удалить на проде)

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

#endregion

await app.SeedDatabaseAsync();

app.UseHttpsRedirection();
app.UseCors(ServiceCollectionExtension.FRONTEND_POLICY_CORS_NAME);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();