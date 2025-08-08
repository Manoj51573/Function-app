using System;
using System.Collections.Generic;
using System.Linq;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Specifications;
using Xunit;

namespace DoT.Eforms.Test.Specifications;

public class E29OverDueSpecificationTest
{
    [Theory]
    [MemberData(nameof(TestParams))]
    public void MatchesExpectedNumberOfItems(E29OverDueSpecification specification, int expectedCount)
    {
        var result = GetTestCollection()
            .AsQueryable()
            .Where(specification.Criteria);

        Assert.Equal(expectedCount, result.Count());
    }

    public static IEnumerable<Object[]> TestParams()
    {
        var now = DateTime.Now;
        return new []
        {
            new object[] { new E29OverDueSpecification(now.AddDays(-5), now.AddDays(-2)), 2 },
            new object[] { new E29OverDueSpecification(now.AddDays(-6), now.AddDays(-10)), 1 },
            new object[] { new E29OverDueSpecification(now.AddDays(-8), now.AddDays(-7)), 2 },
            new object[] { new E29OverDueSpecification(now.AddDays(-5), now.AddDays(-11)), 1 },
        };
    }

    private List<FormInfo> GetTestCollection()
    {
        var now = DateTime.Now;
        return new List<FormInfo>
        {
            new()
            {
                AllFormsId = (int)FormType.e29, Created = now.AddDays(-3), FormStatusId = (int)FormStatus.Unsubmitted
            },
            new()
            {
                AllFormsId = (int)FormType.CoI, Created = now.AddDays(-2), FormStatusId = (int)FormStatus.Unsubmitted
            },
            new()
            {
                AllFormsId = (int)FormType.e29, Created = now.AddDays(-5), FormStatusId = (int)FormStatus.Unsubmitted
            },
            new()
            {
                AllFormsId = (int)FormType.e29, Created = now.AddDays(-2), FormStatusId = (int)FormStatus.Unsubmitted
            },
            new()
            {
                AllFormsId = (int)FormType.e29, Created = now.AddDays(-7), FormStatusId = (int)FormStatus.Unsubmitted
            },
            new()
            {
                AllFormsId = (int)FormType.e29, Created = now.AddDays(-4), FormStatusId = (int)FormStatus.Unsubmitted
            },
            new()
            {
                AllFormsId = (int)FormType.CoI, Created = now.AddDays(-5), FormStatusId = (int)FormStatus.Unsubmitted
            },
            new()
            {
                AllFormsId = (int)FormType.e29, Created = now.AddDays(-8), FormStatusId = (int)FormStatus.Unsubmitted
            },
            new()
            {
                AllFormsId = (int)FormType.e29, Created = now.AddDays(-10), FormStatusId = (int)FormStatus.Unsubmitted
            },
        };
    }
}