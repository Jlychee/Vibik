using DotNetEnv;
using Npgsql;

namespace Infrastructure;

public static class DataBaseManager
{
    private static string dbPassword;
    private static string dbUser;
    private static string dbName;
    private static int dbPort;
    private static string dbHost;
    private static string dbConnectionString;

    private static void DataBaseInitialize()
    {
        Env.Load();
        dbHost = Env.GetString("DB_HOST");
        dbPort = Env.GetInt("DB_PORT");
        dbName = Env.GetString("DB_NAME");
        dbUser = Env.GetString("DB_USER");
        dbPassword = Env.GetString("DB_PASSWORD");
        dbConnectionString = $"server={dbHost};port={dbPort};database={dbName};user={dbUser};password={dbPassword}";
    }


    public static bool CheckDbConnection()
    {
        var sqlConnection = new NpgsqlConnection(dbConnectionString);
        sqlConnection.Open();
        var dbIsOpen = sqlConnection.State == System.Data.ConnectionState.Open;
        sqlConnection.Close();
        return dbIsOpen;
    }
}