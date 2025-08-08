using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Specifications;
using Xunit;

namespace DoT.Eforms.Test.Specifications;

public class EmployeeSearchSpecificationTest
{
    [Theory(Skip = "Requires integration test as EF.Functions no longer supported")]
    [MemberData(nameof(TestParams))]
    public async Task EmployeeSearch_should_return_expected_results(EmployeeSearchSpecification spec, int expectedCount)
    {
        var result = GetTestCollection()
            .AsQueryable()
            .Where(spec.Criteria)
            .Skip(spec.Skip ?? 0)
            .Take(spec.Take ?? 5);
        
        Assert.Equal(expectedCount, result.Count());
    }

    [Fact(Skip = "Requires integration test as EF.Functions no longer supported")]
    public async Task EmployeeSearch_should_skip_results_when_requested()
    {
        var spec = new EmployeeSearchSpecification("a", take: 1, skip: 4);
        var query = GetTestCollection()
            .AsQueryable()
            .Where(spec.Criteria)
            .Skip(spec.Skip!.Value)
            .Take(spec.Take!.Value);

        var result = Assert.Single(query);
        Assert.Equal("test3@email", result.EmployeeEmail);
        Assert.Equal("Test", result.EmployeeFirstName);
        Assert.Equal("2", result.EmployeeSurname);
    }

    public static object[][] TestParams()
    {
        return new []
        {
            new object[] { new EmployeeSearchSpecification("a"), 5 },
            new object[] { new EmployeeSearchSpecification("mango.berry@"), 1 },
            new object[] { new EmployeeSearchSpecification("davie"), 1 },
            new object[] { new EmployeeSearchSpecification("arr"), 1 },
            new object[] { new EmployeeSearchSpecification("eRR"), 1 },
            new object[] { new EmployeeSearchSpecification("tESt"), 3 },
            new object[] { new EmployeeSearchSpecification("a", take: 2), 2 },
        };
    }
    
    private List<AdfUser> GetTestCollection()
    {
        return new List<AdfUser>
        {
            new (){ EmployeeEmail = "test@email", EmployeeFirstName = "Some", EmployeeSurname = "Davies", EmployeeSecondName = "" },
            new (){ EmployeeEmail = "other.hughes@email", EmployeeFirstName = "Other", EmployeeSurname = "Hughes", EmployeeSecondName = "Warren" },
            new (){ EmployeeEmail = "Mango.Berry@email", EmployeeFirstName = "Mango", EmployeeSurname = "Berry", EmployeeSecondName = "Crunch" },
            new (){ EmployeeEmail = "test2@email", EmployeeFirstName = "Test", EmployeeSurname = "1", EmployeeSecondName = "" },
            new (){ EmployeeEmail = "test3@email", EmployeeFirstName = "Test", EmployeeSurname = "2", EmployeeSecondName = "" }
        };
    }
}