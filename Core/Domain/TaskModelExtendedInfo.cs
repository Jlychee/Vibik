namespace Domain.Models;

public class TaskModelExtendedInfo
{
    public string Description { get; set; }
    public int PhotosRequired { get; set; }
    public List<Uri>? ExamplePhotos { get; set; }
    public List<Uri> UserPhotos { get; set; }
}