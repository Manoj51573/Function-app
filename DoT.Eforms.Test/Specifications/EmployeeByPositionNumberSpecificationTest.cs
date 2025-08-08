using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Specifications;
using Xunit;

namespace DoT.Eforms.Test.Specifications;

public class EmployeeByPositionNumberSpecificationTest
{
    [Fact]
    public async Task Position_WhenHasSingleEmployee_ReturnsOneEmployee()
    {
        var spec = new EmployeeByPositionNumberSpecification(12345);
        var result = GetTestCollection()
            .AsQueryable()
            .Where(spec.Criteria);
        
        Assert.Collection(result, user =>
            Assert.Equal("1", user.EmployeeNumber)
        );
    }

    [Fact]
    public async Task Position_WhenHasMultipleEmployee_ReturnsAllEmployees()
    {
        var spec = new EmployeeByPositionNumberSpecification(543210);
        var result = GetTestCollection()
            .AsQueryable()
            .Where(spec.Criteria);
        
        Assert.Collection(result, 
            user => Assert.Equal("2", user.EmployeeNumber), user => Assert.Equal("3", user.EmployeeNumber));
    }
    
    [Fact]
    public async Task Position_WhenHasMultipleEmployeeEvenSubstantive_ReturnsAllEmployees()
    {
        var spec = new EmployeeByPositionNumberSpecification(543217);
        var result = GetTestCollection()
            .AsQueryable()
            .Where(spec.Criteria);
        
        Assert.Collection(result, 
            user => Assert.Equal("4", user.EmployeeNumber), user => Assert.Equal("5", user.EmployeeNumber));
    }

    private List<AdfUser> GetTestCollection()
    {
        var position1 = new AdfPosition { Id = 12345, OccupancyType = "HDA" };
        var position2 = new AdfPosition { Id = 543210, OccupancyType = null };
        var position3 = new AdfPosition { Id = 543217, OccupancyType = "SUB" };
        return new List<AdfUser>
        {
            new () { EmployeeNumber="1", Position = position1, PositionId = position1.Id },
            new() { EmployeeNumber = "2", Position = position2, PositionId = position2.Id },
            new() { EmployeeNumber = "3", Position = position2, PositionId = position2.Id },
            new() { EmployeeNumber = "4", IsSubstantive = false, Position = position3, PositionId = position3.Id },
            new() { EmployeeNumber = "5", IsSubstantive = true, Position = position3, PositionId = position3.Id }
        };
    }
}