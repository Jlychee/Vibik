using Vibik.Core.Domain;

namespace Vibik.Core.Application.Interfaces;

public interface IUserTasksTable
{
    public static bool AddTask(int id, string title, DateTime startTime, string status, out TaskItem task)
    {
        throw new NotImplementedException();
    }

    public static bool GetTasksList(out ISet<string> tasks)
    {
        throw new NotImplementedException();
    }

    public static bool GetTaskById(int id, out TaskItem task)
    {
        throw new NotImplementedException();
    }
}