using static Infrastructure.DataBaseManager;

namespace Tests;

[TestFixture]
public class DataBaseTest
{
    [Test]
    public void CheckDataBaseConnection()
    {
        DataBaseInitialize();
        Assert.That(CheckDbConnection(), Is.True);
    }
}