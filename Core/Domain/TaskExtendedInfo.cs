namespace Shared.Models;

public class TaskExtendedInfo
{
    public string Description { get; set; }
    public int PhotosRequired { get; set; }
    public List<Uri>? ExamplePhotos { get; set; }
    public List<Uri> UserPhotos { get; set; }

    public void AddPhoto(Uri photo) => UserPhotos.Add(photo);
}