using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications
{
    public class GetHistoryByActionAndFormIdDescending : BaseQuerySpecification<FormHistory>
    {
        public GetHistoryByActionAndFormIdDescending(int formInfoId, string action) : base(
            x => x.FormInfoId == formInfoId && x.ActionType == action, x => x.Created, orderDescending: true)
        {
            
        }
    }
}