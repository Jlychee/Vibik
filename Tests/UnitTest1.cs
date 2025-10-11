using Vibik.Core.Domain;

namespace Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void SetPassword_And_Verify_Success()
    {
        var user = new User {Name = "Bobik"};
        
        user.SetPassword("12345");

        Assert.That(user.VerifyPassword("1234"), Is.False);
        Assert.That(user.VerifyPassword("12345"), Is.True);
    }
}