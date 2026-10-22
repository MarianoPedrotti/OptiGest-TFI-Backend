using OptiGest.Tests.Sql;

namespace OptiGest.Tests.Unit;

[Trait("Suite", "Unit")]
public class IsolationTests
{
    [Theory]
    [InlineData("Production", "OptiGest_Testing_Phase05", "OptiGest_Testing_Phase05_")]
    [InlineData("Testing", "SQLEXPRESS", "OptiGest_Testing_Phase05_")]
    [InlineData("Testing", "MSSQLLocalDB", "OptiGest_Testing_Phase05_")]
    [InlineData("Testing", "OptiGest_Testing_Phase05", "Ventas_")]
    public void Foreign_configuration_is_rejected_before_any_connection(string environment, string instance, string prefix)
        => Assert.Throws<InvalidOperationException>(() => new TestingSettings(environment, instance, prefix).Validate());

    [Theory]
    [InlineData("master")]
    [InlineData("OptiGest")]
    [InlineData("OptiGest_Testing_Phase05_abc;DROP DATABASE master")]
    public void Foreign_database_names_are_rejected(string name)
        => Assert.Throws<InvalidOperationException>(() => TestingDatabase.ValidateDatabaseName(name));
}
