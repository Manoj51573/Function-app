using System;
using System.Collections.Generic;
using System.Linq;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Specifications;
using Xunit;

namespace DoT.Eforms.Test.Specifications
{
    public class FormsTypeFromPeriodSpecificationTest
    {
        [Theory]
        [MemberData(nameof(TestParams))]
        public void MatchesExpectedNumberOfItems(FormsTypeFromPeriodSpecification specification, DateTime expectedCreated, FormType expectedFormType)
        {;
            var result = GetTestCollection()
                .AsQueryable()
                .Where(specification.Criteria);

            var formInfo = Assert.Single(result);
            Assert.Equal((int)expectedFormType, formInfo.AllFormsId);
            Assert.Equal(expectedCreated, formInfo.Created);
        }

        public static IEnumerable<Object[]> TestParams()
        {
            return new []
            {
                new object[] { new FormsTypeFromPeriodSpecification((int)FormType.e29, new DateTime(2022, 1, 7), null), new DateTime(2022, 1, 15), FormType.e29 },
                new object[] { new FormsTypeFromPeriodSpecification((int)FormType.e29, null, new DateTime(2022, 1, 1)), new DateTime(2021, 12, 15), FormType.e29 },
                new object[] { new FormsTypeFromPeriodSpecification((int)FormType.CoI, new DateTime(2022, 1, 1), null), new DateTime(2022, 1, 9), FormType.CoI },
                new object[] { new FormsTypeFromPeriodSpecification((int)FormType.e29, new DateTime(2022, 1, 1), new DateTime(2022, 1, 14)), new DateTime(2022, 1, 6), FormType.e29 },
            };
        }

        private static IEnumerable<FormInfo> GetTestCollection()
        {
            return new List<FormInfo>
            {
                new ()
                {
                    AllFormsId = (int)FormType.e29, Created = new DateTime(2021, 12, 15)
                },
                new ()
                {
                    AllFormsId = (int)FormType.e29, Created = new DateTime(2022, 1, 15)
                },
                new ()
                {
                    AllFormsId = (int)FormType.CoI, Created = new DateTime(2022, 1, 9)
                },
                new ()
                {
                    AllFormsId = (int)FormType.e29, Created = new DateTime(2022, 1, 6)
                },
            };
        }
    }
}