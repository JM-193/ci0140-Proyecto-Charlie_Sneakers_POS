using Microsoft.Data.SqlClient;

namespace CharliesSneakersPOS.IntegrationTests;

// Builds the connection string for the temporary test databases.
// Order of precedence:
//   1. TEST_SQL_CONNECTION: full connection string without database
//      (used by CI against SQL Server in Docker, SQL authentication).
//   2. TEST_SQL_SERVER: server name only, Windows authentication.
//   3. Default: (localdb)\MSSQLLocalDB, Windows authentication.
internal static class TestSqlServer
{
    private const string DefaultServer = @"(localdb)\MSSQLLocalDB";

    public static string ConnectionString(string databaseName)
    {
        var connection = Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION");

        if (!string.IsNullOrWhiteSpace(connection))
        {
            return new SqlConnectionStringBuilder(connection)
            {
                InitialCatalog = databaseName
            }.ConnectionString;
        }

        var server = Environment.GetEnvironmentVariable("TEST_SQL_SERVER");

        return new SqlConnectionStringBuilder
        {
            DataSource = string.IsNullOrWhiteSpace(server) ? DefaultServer : server,
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true
        }.ConnectionString;
    }
}
