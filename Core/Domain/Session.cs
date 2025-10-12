namespace Vibik.Core.Domain;

public class Session
{
    public User User { get; private set; }
    public MapConfiguration MapConfig { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public Session(User user, MapConfiguration mapConfig)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        
        if (mapConfig == null)
            throw new ArgumentNullException(nameof(mapConfig));

        User = user;
        MapConfig = mapConfig;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void UpdateMapConfiguration(MapConfiguration newConfig)
    {
        if (newConfig == null)
            throw new ArgumentNullException(nameof(newConfig));
            
        MapConfig = newConfig;
    }
}