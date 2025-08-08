using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DoT.Infrastructure.Interfaces;

namespace DoT.Infrastructure.DbModels;

public class EmployeeDetailsDto : IUserInfo
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
    public string Department { get; set; }
    public string Branch { get; set; }
    public string Section { get; set; }
    public string Unit { get; set; }
    public int? EmployeeManagementTier { get; set; }
    public string EmployeeEmail { get; set; }
    public bool EmployeeOnLeave { get; set; }

    public string EmployeePreferredFullName => string.Join(' ', EmployeePreferredName, EmployeeSurname);
    
    [NotMapped]
    public IList<IUserInfo> Managers { get; set; } = new List<IUserInfo>();
    [NotMapped]
    public IList<IUserInfo> ExecutiveDirectors { get; set; } = new List<IUserInfo>();

    public int? ManagerPositionId => Managers.FirstOrDefault()?.EmployeePositionId;

    public string ManagerIdentifier => Managers.Count == 1
        ? Managers.Single().EmployeeEmail
        : Managers.FirstOrDefault()?.EmployeePositionTitle;
    
    public int? ExecutiveDirectorPositionId => ExecutiveDirectors.FirstOrDefault()?.EmployeePositionId;

    public string ExecutiveDirectorIdentifier => ExecutiveDirectors.Count == 1
        ? ExecutiveDirectors.Single().EmployeeEmail
        : ExecutiveDirectors.FirstOrDefault()?.EmployeePositionTitle;
}