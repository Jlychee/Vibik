using Task = System.Threading.Tasks.Task;

namespace Core.Interfaces;

public interface IAuthNavigator
{
    Task RedirectToLoginAsync();
}
