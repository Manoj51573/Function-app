using System;
using System.Collections.Generic;
using System.Linq;

namespace DoT.Infrastructure.Interfaces;

public interface IUserInfo
{
    public Guid ActiveDirectoryId { get; set; }
    public string EmployeePreferredName { get; set; }
    public string EmployeeFirstName { get; set; }
    public string EmployeeSurname { get; set; }
    public string EmployeeNumber { get; set; }
    public string EmployeePositionNumber { get; set; }
    public int? EmployeePositionId { get; set; }
    public string EmployeePositionTitle { get; set; }
    public string Directorate { get; set; }
    public int? EmployeeManagementTier { get; set; }
    public string EmployeeEmail { get; set; }
    public string? EmployeePreferredFullName => string.Join(' ', EmployeePreferredName, EmployeeSurname);
    public IList<IUserInfo> Managers => new List<IUserInfo>();
    public IList<IUserInfo> ExecutiveDirectors => new List<IUserInfo>();
    public int? ManagerPositionId => Managers.FirstOrDefault()?.EmployeePositionId;

    public string ManagerIdentifier => Managers.Count == 1
        ? Managers.Single().EmployeeEmail
        : Managers.FirstOrDefault()?.EmployeePositionTitle;

    public int? ManagerManagementTier => Managers.FirstOrDefault()?.EmployeeManagementTier;
    
    public int? ExecutiveDirectorPositionId => ExecutiveDirectors.FirstOrDefault()?.EmployeePositionId;

    public string ExecutiveDirectorIdentifier => ExecutiveDirectors.Count == 1
        ? ExecutiveDirectors.Single().EmployeeEmail
        : ExecutiveDirectors.FirstOrDefault()?.EmployeePositionTitle;

    public string ExecutiveDirectorName => ExecutiveDirectors.Count == 1
        ? ExecutiveDirectors.Single().EmployeePreferredFullName
        : ExecutiveDirectors.FirstOrDefault()?.EmployeePositionTitle;

    public string ManagerName => Managers.Count == 1 ? Managers.Single().EmployeeEmail :
        Managers.FirstOrDefault()?.EmployeePositionTitle;
}