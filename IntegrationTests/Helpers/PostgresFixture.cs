using Education.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IntegrationTests.Helpers;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly string _databaseName = $"education_it_{Guid.NewGuid():N}";
    private string _adminConnection = null!;
    private bool _created;
    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Only the server credentials are reused. Never migrate or reset the supplied database.
        var settings = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("EDUCATION_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Username=postgres;Password=1234")
        { Database = "postgres", Pooling = false, Timeout = 5 };
        _adminConnection = settings.ConnectionString;
        await ExecuteOnAdminAsync($"CREATE DATABASE \"{_databaseName}\"");
        _created = true;
        settings.Database = _databaseName;
        ConnectionString = settings.ConnectionString;
        await using var db = new EducationDbContext(new DbContextOptionsBuilder<EducationDbContext>()
            .UseNpgsql(ConnectionString).Options);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_created)
            await ExecuteOnAdminAsync($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)");
    }

    private async Task ExecuteOnAdminAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class AuthIntegrationCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "HTTP integration";
}
