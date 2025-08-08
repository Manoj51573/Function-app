using System;
using System.Collections.Generic;
using System.Linq;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Specifications;
using Xunit;

namespace DoT.Eforms.Test.Specifications
{
    public class GetHistoryByActionAndFormIdDescendingTest
    {
        [Theory]
        [InlineData(1, FormStatus.Submitted, "User 3")]
        [InlineData(1, FormStatus.Delegate, "User 1")]
        [InlineData(2, FormStatus.Submitted, "User 2")]
        public void MatchesExpectedNumberOfItems(int formId, FormStatus status, string expectedUser)
        {
            var action = Enum.GetName(typeof(FormStatus), status);
            var spec = new GetHistoryByActionAndFormIdDescending(formId, action);
            var result = GetTestCollection()
                .AsQueryable()
                .Where(spec.Criteria);
            
            var onlyResult = Assert.Single(result);
            Assert.Equal(expectedUser,  onlyResult.ActionBy);
        }
        
        private List<FormHistory> GetTestCollection()
        {
            return new List<FormHistory>
            {
                new()
                {
                    FormInfoId = 1, ActionType = Enum.GetName(typeof(FormStatus), FormStatus.Unsubmitted),
                    FormStatusId = (int)FormStatus.Unsubmitted, FormHistoryId = 1, ActionBy = "User 1"
                },
                new()
                {
                    FormInfoId = 2, ActionType = Enum.GetName(typeof(FormStatus), FormStatus.Submitted),
                    FormStatusId = (int)FormStatus.Submitted, FormHistoryId = 2, ActionBy = "User 2"
                },
                new()
                {
                    FormInfoId = 1, ActionType = Enum.GetName(typeof(FormStatus), FormStatus.Delegate),
                    FormStatusId = (int)FormStatus.Unsubmitted, FormHistoryId = 3, ActionBy = "User 1"
                },
                new()
                {
                    FormInfoId = 1, ActionType = Enum.GetName(typeof(FormStatus), FormStatus.Unsubmitted),
                    FormStatusId = (int)FormStatus.Unsubmitted, FormHistoryId = 4, ActionBy = "User 3"
                },
                new()
                {
                    FormInfoId = 1, ActionType = Enum.GetName(typeof(FormStatus), FormStatus.Submitted),
                    FormStatusId = (int)FormStatus.Submitted, FormHistoryId = 5, ActionBy = "User 3"
                },
            };
        }
    }
}