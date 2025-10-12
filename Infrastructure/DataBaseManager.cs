using DotNetEnv;
using Npgsql;

namespace Infrastructure;

public static class DataBaseManager
{
    private static string dbConnectionString;

    public static void DataBaseInitialize()
    {
        dbConnectionString =
            $"server={Env.GetString("DB_HOST")};" +
            $" port={Env.GetInt("DB_PORT")};" +
            $" database={Env.GetString("DB_NAME")};" +
            $" username={Env.GetString("DB_USER")};" +
            $" password={Env.GetString("DB_PASSWORD")}";
    }


    public static bool CheckDbConnection()
    {
        var sqlConnection = new NpgsqlConnection(dbConnectionString);
        sqlConnection.Open();
        var dbIsOpen = sqlConnection.State == System.Data.ConnectionState.Open;
        sqlConnection.Close();
        return dbIsOpen;
    }

    private class DataBaseResponse(string response, string error, bool isSuccess)
    {
        public string Response { get; private set; } = response;
        public string Error { get; private set; } = error;
        public bool IsSuccess { get; private set; } = isSuccess;
    }
}