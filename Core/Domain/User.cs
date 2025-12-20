namespace Core.Domain;

public class User
{
    public required string Username { get; set; }
    public required string DisplayName { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int Money { get; set; }
}