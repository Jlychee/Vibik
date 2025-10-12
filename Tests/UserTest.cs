using Vibik.Core.Domain;

namespace Tests;

[TestFixture]
public class UserTest
{
    [Test]
    public void Sets_Name_Normalized_Any_PasswordHash()
    {
        var user = new User(" Бобик_1999 ", "Бобик");

        Assert.Multiple(() =>
        {
            Assert.That(user.DisplayName, Is.EqualTo("Бобик"));
            Assert.That(user.Username, Is.EqualTo("бобик_1999"));
        });
        
        user.ChanheDisplayName("Бобик1");
        Assert.Multiple(() =>
        {
            Assert.That(user.DisplayName, Is.EqualTo("Бобик1"));
            Assert.That(user.Username, Is.EqualTo("бобик_1999"));
        });
    }
}