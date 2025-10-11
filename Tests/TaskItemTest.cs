using Vibik.Core.Domain;

namespace Tests;

[TestFixture]
public class TaskItemTest
{
    [Test]
    public void Complete_Sets_IsCompleted()
    {
        var task = new TaskItem
        {
            OwnerName = "бобик",
            Title = "take the photo of the sunset",
            TaskName = "Сфоткать закат"
        };
        
        Assert.That(task.IsCompleted, Is.False);
        task.Complete();
        Assert.That(task.IsCompleted, Is.True);
    }

    [Test]
    public void setAward_Sets_Value_And_Throws_On_Negative()
    {
        var task = new TaskItem { OwnerName = "бобик", Title = "task1" };

        task.SetAward(10);
        Assert.That(task.Award, Is.EqualTo(10));
        
        Assert.Throws<ArgumentOutOfRangeException>(() => task.SetAward(-1));
    }
    
    [Test]
    public void Tags_Are_CaseInsensitive_And_No_Duplicates()
    {
        var task = new TaskItem { OwnerName = "kate", Title = "read-book" };

        task.Tags.Add("home");
        task.Tags.Add("Home");
        task.Tags.Add("HOME");

        Assert.That(task.Tags, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(task.Tags.Contains("home"), Is.True);
            Assert.That(task.Tags.Contains("HOME"), Is.True);
        });
    }

    [Test]
    public void Title_And_TaskName_Can_Differ()
    {
        var t = new TaskItem
        {
            OwnerName = "бобик",
            Title = "task1",
            TaskName = "Сфоткала?"
        };

        Assert.Multiple(() =>
        {
            Assert.That(t.Title, Is.EqualTo("task1"));
            Assert.That(t.TaskName, Is.EqualTo("Сфоткала?"));
        });
    }
}