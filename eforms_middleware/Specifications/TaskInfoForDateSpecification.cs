using System;
using System.Collections.Generic;
using System.Linq;
using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications;

public class TaskInfoForDateSpecification : BaseQuerySpecification<TaskInfo>
{
    public TaskInfoForDateSpecification(int? formInfoId = null, DateTime? taskDate = null, IList<int> excludeTypes = null, bool activeOnly = false)
        : base(
            x => (!formInfoId.HasValue || x.FormInfoId == formInfoId) &&
                 (!taskDate.HasValue || taskDate == x.SpecialReminderDate || taskDate == x.EscalationDate) &&
                 (excludeTypes == null || excludeTypes.All(e => e != x.AllFormsId)) &&
                 (!activeOnly || x.ActiveRecord == true),
            x => x.TaskCreatedDate, orderDescending: true)
    {
        AddInclude(x => x.FormInfo);
    }
}