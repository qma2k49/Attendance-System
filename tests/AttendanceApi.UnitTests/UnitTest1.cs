using Xunit;
using Microsoft.EntityFrameworkCore;
using AttendanceApi.Infrastructure.Data;

namespace AttendanceApi.UnitTests;

public class Sprint0SetupTests
{
    [Fact]
    public void Test_Environment_And_DbContext_Initialization()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Db")
            .Options;

        using var context = new AttendanceDbContext(options);
        
        Assert.NotNull(context);
    }
}