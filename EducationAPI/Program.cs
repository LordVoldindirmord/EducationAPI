using EducationAPI.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
// Dependency Injection
builder.Services.AddEducationServices(builder.Configuration);
// Регистрация Identity
builder.Services.AddIdentity();
// Добавление авторизации
builder.Services.AddAuthorizationPolicies();
// Добавление нормального парсинга Enum в JSON (без этого enum будет конвертироваться в число)
builder.Services.AddControllersWithJson();
// Нужно для нормальной работы с React
builder.Services.AddFrontendCors();

var app = builder.Build();

#region Development (удалить на проде)

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

#endregion

// Запуск всех сидеров
await app.SeedDatabaseAsync();

app.UseHttpsRedirection();
app.UseCors(ServiceCollectionExtension.FRONTEND_POLICY_CORS_NAME);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();