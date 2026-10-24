using Npgsql;

namespace IntegrationTests.Helpers
{
    /// <summary>
    /// Поднимает отдельную PostgreSQL-базу для интеграционных тестов,
    /// чтобы не затрагивать основную базу приложения.
    /// </summary>
    public sealed class PostgresFixture : IAsyncLifetime
    {
        private const string Host = "localhost";
        private const int Port = 5432;
        private const string Username = "postgres";
        private const string Password = "1234";

        private readonly string _databaseName = $"education_it_{Guid.NewGuid():N}";

        public string ConnectionString { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await ExecuteOnAdminAsync($@"CREATE DATABASE ""{_databaseName}""");

            ConnectionString =
                $"Host={Host};Port={Port};Database={_databaseName};Username={Username};Password={Password}";

            await AuthTestHost.InitializeDatabaseAsync(ConnectionString);
        }

        public async Task DisposeAsync()
        {
            await ExecuteOnAdminAsync(
                $@"SELECT pg_terminate_backend(pid)
                   FROM pg_stat_activity
                   WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid();");

            await ExecuteOnAdminAsync($@"DROP DATABASE IF EXISTS ""{_databaseName}""");
        }

        private static async Task ExecuteOnAdminAsync(string sql)
        {
            var adminConnection =
                $"Host={Host};Port={Port};Database=postgres;Username={Username};Password={Password}";

            await using var connection = new NpgsqlConnection(adminConnection);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    [CollectionDefinition(Name)]
    public sealed class AuthIntegrationCollection : ICollectionFixture<PostgresFixture>
    {
        public const string Name = "AuthIntegration";
    }
}