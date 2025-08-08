using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.DataModel;

namespace eforms_middleware.Interfaces;

public interface IFormHistoryService
{
    Task<List<FormHistoryDto>> GetFormHistoryDetailsByID(int formInfoId);

    Task<FormHistory> GetFirstOrDefaultHistoryBySpecification(
        ISpecification<FormHistory> specification);

    Task AddFormHistoryAsync(FormInfoUpdate request, FormInfo form, Guid? actioningGroup, string additionalInformation, string ReasonForDecision= "");
}