namespace Vibik.Core.Domain;

public class User
{
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public int Experience { get; set; }
    public ICollection<TaskItem> TaskItems { get; private set; } = new List<TaskItem>();

    protected User()
    {
    }

    public User(string name) => SetName(name);

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя обязательно", nameof(name));
        Name = name.Trim();
        NormalizedName = Normalize(name);
    }

    private static string Normalize(string name) => name.Trim().ToLowerInvariant();
    
    internal void SetTasks(ICollection<TaskItem> tasks) => TaskItems = tasks;
    public void AddExperience(int experience) => Experience += experience;
}