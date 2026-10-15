using Microsoft.Data.Sqlite;

namespace Broker;

public class DatabaseInitializer
{
    public static string GetDatabasePath()
    {
        string? envPath = Environment.GetEnvironmentVariable("DB_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            string fullPath = Path.GetFullPath(envPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            return fullPath;
        }

        string dataDir = Path.Combine(GetProjectRoot(), "Data");
        Directory.CreateDirectory(dataDir);

        return Path.Combine(dataDir, "broker_messages.db");
    }
    public static void CreateTables(SqliteConnection connection)
    {
        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode=DELETE;";
            pragma.ExecuteNonQuery();
        }

        using var create = connection.CreateCommand();
        create.CommandText =
            @"CREATE TABLE IF NOT EXISTS users (
                Username TEXT PRIMARY KEY,
                PasswordHash TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS messages (
                Id TEXT PRIMARY KEY,
                Topic TEXT NOT NULL,
                Message TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS subscriptions (
                ClientId TEXT NOT NULL,
                Topic TEXT NOT NULL,
                PRIMARY KEY (ClientId, Topic)
            );

            CREATE TABLE IF NOT EXISTS deliveries (
                MessageId TEXT NOT NULL,
                ClientId TEXT NOT NULL,
                Status INTEGER NOT NULL,
                PRIMARY KEY (MessageId, ClientId)
            );";
        create.ExecuteNonQuery();
    }

    private static string GetProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && dir.GetFiles("*.csproj").Length == 0)
            dir = dir.Parent;

        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}