using Vibik.Core.Domain;

namespace Vibik.Core.Application.Interfaces;

public interface IUserTable
{
    public static bool TryRegister(string nickname, string hashPassword, out User user)
    {
        throw new NotImplementedException();
    }

    public static bool TryLogin(string nickname, string hashPassword, out User user)
    {
        throw new NotImplementedException();
    }

    public static User TryAuth(string nickname, string hashPassword, out User user)
    {
        throw new NotImplementedException();
    }

    public static int GetUserExp(string nickname)
    {
        throw new NotImplementedException();
    }
}