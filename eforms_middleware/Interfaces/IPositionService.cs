using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.DataModel;
using System;
using System.Threading.Tasks;

namespace eforms_middleware.Interfaces;

public interface IPositionService
{
    Task<AdfPosition> GetPositionByIdAsync(int positionId, bool isIncludeUsers = true);
    Task<AdfPosition> GetPositionByUsersAdIdAsync(Guid id);
    Task<BusCaseNonAdvPosition> GetBusinessCasePositionDetail(int positionId, bool isIncludeUsers = true);
}