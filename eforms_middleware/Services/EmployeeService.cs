using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eforms_middleware.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ISpViewRepository<EmployeeDetailsDto> _spViewRepository;
    private readonly IRepository<AdfUser> _adfUserRepository;
    private readonly IRepository<AdfPosition> _adfPositionRepository;
    private readonly IRepository<AdfGroupMember> _adfGroupMemberRepository;
    private readonly IRepository<AdfGroup> _adfGroupRepository;
    private readonly ILogger<EmployeeService> _logger;



    public EmployeeService(ILogger<EmployeeService> logger,
        ISpViewRepository<EmployeeDetailsDto> spViewRepository, IRepository<AdfUser> adfUserRepository,
        IRepository<AdfPosition> adfPositionRepository, IRepository<AdfGroupMember> adfGroupMemberRepository, IRepository<AdfGroup> adfGroupRepository)
    {
        _spViewRepository = spViewRepository;
        _adfUserRepository = adfUserRepository;
        _adfPositionRepository = adfPositionRepository;
        _logger = logger;
        _adfGroupMemberRepository = adfGroupMemberRepository;
        _adfGroupRepository = adfGroupRepository;
    }

    public async Task<IUserInfo> GetEmployeeDetailsAsync(string employeeEmail)
    {
        // Will exclude employees that have EmployeeOccupancyType of null
        return await GetEmployeeByEmailAsync(employeeEmail);
    }

    public async Task<IList<IUserInfo>> GetEmployeeByPositionNumberAsync(int positionNumber)
    {
        try
        {
            var specification = new EmployeeByPositionNumberSpecification(positionNumber);
            var users = await _adfUserRepository.ListAsync(specification);
            return users.Select(x => x.ToUserInfo()).ToList();
        }
        catch
        {
            return null;
        }
    }

    public async Task<IUserInfo> GetEmployeeByAzureIdAsync(Guid azureId)
    {
        var user = await _adfUserRepository.SingleOrDefaultAsync(new EmployeeSpecification(azureId));
        AdfPosition managers = null;
        AdfPosition executiveDirector = null;
        if (user.Position?.ReportsPositionId.HasValue ?? false)
        {
            var specification = new EmployeesManagerAndEdSpecification(user.Position.ReportsPositionId.Value, user.Position.Directorate, user.Position.ManagementTier > 3);
            var positions = await _adfPositionRepository.ListAsync(specification);
            managers = positions.FirstOrDefault(x => x.ManagementTier != 3);
            executiveDirector = positions.FirstOrDefault(x => x.ManagementTier == 3);
        }
        return user.ToUserInfo(managers, executiveDirector);
    }

    public async Task<IUserInfo> GetEmployeeByEmailAsync(string email)
    {
        var userSpecification = new UserSpecification(email);
        var user = await _adfUserRepository.SingleOrDefaultAsync(userSpecification);
        AdfPosition managers = null;
        AdfPosition executiveDirector = null;
        if (user.Position?.ReportsPositionId.HasValue ?? false)
        {
            var specification = new EmployeesManagerAndEdSpecification(user.Position.ReportsPositionId.Value, user.Position.Directorate, user.Position.ManagementTier > 3);
            var positions = await _adfPositionRepository.ListAsync(specification);
            managers = positions.FirstOrDefault(x => x.ManagementTier != 3);
            executiveDirector = positions.FirstOrDefault(x => x.ManagementTier == 3);
        }
        return user.ToUserInfo(managers, executiveDirector);
    }

    public async Task<IList<IUserInfo>> FindEmployeesAsync(IQueryCollection query)
    {
        string searchQuery = query["searchQuery"];
        var includeContractors = query.ContainsKey("includeContractors");
        int.TryParse(query["skip"], out var skip);
        int? take = null;
        if (int.TryParse(query["take"], out var tempTake)) take = tempTake;
        var searchSpecification = new EmployeeSearchSpecification(searchQuery: searchQuery, includeContractors: includeContractors, skip: skip, take: take);
        var result = await _adfUserRepository.ListAsync(searchSpecification);
        return result.Select(x => x.ToUserInfo()).ToList();
    }

    public async Task<bool> IsPodGroupUser(Guid azureId)
    {
        var groupMember = await _adfGroupMemberRepository.FindByAsync(x => x.MemberId == azureId);
        var result = groupMember != null && groupMember.Any()
                    && groupMember.Any(x => x.GroupId == ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
        return result;
    }

    public async Task<bool> IsPodUserGroupEmail(string email)
    {
        var groupMember = await _adfGroupRepository.FindByAsync(x => x.GroupEmail == email);
        return groupMember.Any();
    }

    public async Task<bool> IsOdgGroupUser(Guid azureId)
    {
        var groupMember = await _adfGroupMemberRepository.FindByAsync(x => x.MemberId == azureId);
        var result = groupMember != null && groupMember.Any()
                    && groupMember.Any(x => x.GroupId == ConflictOfInterest.ODG_AUDIT_GROUP_ID);
        return result;
    }
}