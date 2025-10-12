using Vibik.Core.Domain;

namespace Tests;

[TestFixture]
public class UserTest
{
    [Test]
    public void Sets_Name_Normalized_Any_PasswordHash()
    {
        var user = new User(" Бобик ");

        Assert.Multiple(() =>
        {
            Assert.That(user.Name, Is.EqualTo("Бобик"));
            Assert.That(user.NormalizedName, Is.EqualTo("бобик"));
        });
    }
    

    [Test]
    public void EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => _ = new User("   "));
    }
}