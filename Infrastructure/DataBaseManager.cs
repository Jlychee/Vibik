using DotNetEnv;
using Npgsql;

namespace Infrastructure;

public static class DataBaseManager
{
    private static string? dbConnectionString;

    public static void DataBaseInitialize()
    {
        LoadEnvironmentFile();

        var host = Env.GetString("DB_HOST");
        var port = Env.GetInt("DB_PORT");
        var database = Env.GetString("DB_NAME");
        var username = Env.GetString("DB_USER");
        var password = Env.GetString("DB_PASSWORD");

        ValidateSetting(host, "DB_HOST");
        ValidateSetting(database, "DB_NAME");
        ValidateSetting(username, "DB_USER");
        ValidateSetting(password, "DB_PASSWORD");

        if (port == 0)
        {
            throw new InvalidOperationException("Environment variable DB_PORT is not configured.");
        }
        dbConnectionString =
            $"server={host};" +
            $" port={port};" +
            $" database={database};" +
            $" username={username};" +
            $" password={password}";
    }
    
    public static bool CheckDbConnection()
    {
        if (string.IsNullOrWhiteSpace(dbConnectionString))
        {
            throw new InvalidOperationException("Database is not initialized. Call DataBaseInitialize() before checking the connection.");
        }

        try
        {
            using var sqlConnection = new NpgsqlConnection(dbConnectionString);
            sqlConnection.Open();
            return sqlConnection.State == System.Data.ConnectionState.Open;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to open database connection with the provided configuration.", ex);
        }
    }

    private static void LoadEnvironmentFile()
    {
        if (File.Exists(".env"))
        {
            Env.Load();
            return;
        }

        var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }
    }

    private static void ValidateSetting(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Environment variable {key} is not configured.");
        }
    }
}