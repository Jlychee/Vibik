using Vibik.Core.Domain;

namespace Tests;

public class Tests
{
    [Test]
    public void SetPassword_And_Verify_Success()
    {
        var user = new User("Bobik", "12345");
        
        user.SetPassword("12345");

        Assert.Multiple(() =>
        {
            Assert.That(user.VerifyPassword("1234"), Is.False);
            Assert.That(user.VerifyPassword("12345"), Is.True);
        });
    }
}