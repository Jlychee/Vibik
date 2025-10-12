using Vibik.Core.Domain;

namespace Vibik.Core.Application.Interfaces;

public interface ITasskTable
{
    public static bool CreateTask(string title, string name, string description, int awards, ISet<string> tags, out TaskItem task)
    {
        throw new NotImplementedException();
    }

    public static bool GetTasksList(out ISet<string> task)
    {
        throw new NotImplementedException();
    }

    public static bool GetTaskByTitle(string title, out TaskItem task)
    {
        throw new NotImplementedException();
    }
}