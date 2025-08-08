using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications;

public class FormHistorySpecification : BaseQuerySpecification<FormHistory>
{
    public FormHistorySpecification(int formId)
    : base(history => history.FormInfoId == formId, history => history.FormHistoryId)
    {
        
    }
}