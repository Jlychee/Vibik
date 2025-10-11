using Vibik.Core.Domain;

namespace Tests;

[TestFixture]
public class UserTest
{
    [Test]
    public void Sets_Name_Normalized_Any_PasswordHash()
    {
        var user = new User(" Бобик ", "Pa$$w0rd");

        Assert.Multiple(() =>
        {
            Assert.That(user.Name, Is.EqualTo("Бобик"));
            Assert.That(user.NormalizedName, Is.EqualTo("бобик"));
            Assert.That(user.VerifyPassword("Pa$$w0rd"), Is.True);
        });
    }

    [Test]
    public void VerifyPassword_Wrong_ReturnFalse()
    {
        var user = new User("Bobik", "1234");
        Assert.That(user.VerifyPassword("12345"), Is.False);
    }

    [Test]
    public void SetPassword_Empty_Throws()
    {
        var u = new User("john", "ok");
        Assert.Throws<ArgumentException>(() => u.SetPassword(""));
    }

    [Test]
    public void EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => _ = new User("   ", "pwd"));
    }
}