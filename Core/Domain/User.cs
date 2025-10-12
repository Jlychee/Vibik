namespace Vibik.Core.Domain;

public class User
{
    public string DisplayName { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public int Experience { get; set; }
    public ICollection<TaskItem> TaskItems { get; private set; } = new List<TaskItem>();

    protected User()
    {
    }

    public User(string username, string displayName)
    {
        Username = username.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
    }
    
    public void ChanheDisplayName(string displayName) => DisplayName = displayName.Trim();
    
    internal void SetTasks(ICollection<TaskItem> tasks) => TaskItems = tasks;
    public void AddExperience(int experience) => Experience += experience;
}