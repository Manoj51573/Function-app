using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using System;
using System.Threading.Tasks;

namespace eforms_middleware.Services;

public class PositionService : IPositionService
{
    private readonly IRepository<AdfPosition> _repository;

    public PositionService(IRepository<AdfPosition> repository)
    {
        _repository = repository;
    }

    public async Task<BusCaseNonAdvPosition> GetBusinessCasePositionDetail(int positionId,
            bool isIncludeUsers = true)
    {
        var specification = new PositionSpecification(positionId, includeUsers: isIncludeUsers);
        var positionDetail = await _repository.SingleOrDefaultAsync(specification);
        var result = new BusCaseNonAdvPosition
        {
            Classification = positionDetail.Classification,
            Directorate = positionDetail.Directorate,
            PositionCreated = positionDetail.PositionCreatedDate,
            PositionNo = positionDetail.PositionNumber,
            PositionStatus = positionDetail.PositionStatus,
            PositionTitle = positionDetail.PositionTitle,
            ReportsTo = positionDetail.ReportsPositionNumber,
            VacantSince = null,
            PositionFTE = string.Empty,
            Branch = string.Empty,
            PositionEndDate = null,
        };
        return result;
    }

    public async Task<AdfPosition> GetPositionByIdAsync(int positionId, bool isIncludeUsers = true)
    {
        var specification = new PositionSpecification(positionId, includeUsers: isIncludeUsers);
        return await _repository.SingleOrDefaultAsync(specification);
    }

    public Task<AdfPosition> GetPositionByUsersAdIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}