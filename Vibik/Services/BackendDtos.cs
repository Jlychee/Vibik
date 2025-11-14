namespace Vibik.Services;

public sealed class UserDto
{
    public string Username { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public int Level { get; set; }
    public int Experience { get; set; }
}

public sealed class PhotoModelDto
{
    public string Url { get; set; } = default!;
}

public sealed class TaskExtendedInfoDto
{
    public string Description { get; set; } = "";
    public int PhotosRequired { get; set; }
    public List<PhotoModelDto> ExamplePhotos { get; set; } = [];
    public List<PhotoModelDto> UserPhotos { get; set; } = [];
}

public sealed class TaskDto
{
    private const double SwapCostModifier = 0.2;
    public string TaskId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime StartTime { get; set; }
    public int Reward { get; set; }
    public TaskExtendedInfoDto? ExtendedInfo { get; set; }

    public int Swap => (int) double.Floor(Reward * SwapCostModifier);
}

public sealed class WhetherDto
{
    
}