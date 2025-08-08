using System;
using System.Collections.Generic;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;

namespace DoT.Eforms.Test.Shared;

public static class CoiTestEmployees
{
    public const string NormalEmployee = "04C76CF3-FF9C-4F1D-BCFA-90C0306391AF";
    public const string NormalManager = "FD4C58E3-63B6-4577-A7ED-93ABFFC63C31";
    public const string NormalExecutiveDirector = "86665A57-0BAE-48B0-A7F6-0E79CE57F567";
    public const string DualManager = "6A1C7722-2357-4168-884C-833D1D867935";
    public const string ManagerlessEmployee = "D47E41AB-D21C-4D45-8345-57323ECC0124";
    public const string ManagerlessAndEdlessEmployee = "FDEDF87A-5C3A-40C0-B9E8-EF4799F41CE6";
    public const string EdlessEmployee = "D9F77005-C0F2-4499-9ED1-2DDD2FE96326";
    public const string EdPAndC = "14EB3853-1466-4372-9197-76D2443137A5";
    public const string DirectorGeneral = "65AC94CA-8BC7-493F-816F-75E441808441";
    public static EmployeeDetailsDto GetEmployeeDetails(Guid employeeId)
    {
        var guidString = employeeId.ToString().ToUpper();
        switch (guidString)
        {
            case NormalEmployee:
                return new EmployeeDetailsDto
                {
                    ActiveDirectoryId = Guid.Parse(guidString),
                    EmployeeEmail = "employee@email",
                    EmployeePreferredName = "Test",
                    EmployeeFirstName = "Test",
                    EmployeeSurname = "Employee",
                    EmployeeOnLeave = false,
                    EmployeePositionId = 1,
                    Managers = new List<IUserInfo>
                    {
                        new EmployeeDetailsDto
                        {
                            EmployeePreferredName = "Manager",
                            EmployeeFirstName = "Manager",
                            EmployeeSurname = "Employee",
                            EmployeeEmail = "ToManager@email",
                            EmployeeManagementTier = 4,
                            EmployeePositionId = 2
                        }
                    },
                    ExecutiveDirectors = new List<IUserInfo>
                    {
                        new EmployeeDetailsDto
                        {
                            EmployeeEmail = "normal_ed@email",
                            EmployeePreferredName = "Exec",
                            EmployeeFirstName = "Executive",
                            EmployeeSurname = "Employee",
                            EmployeeOnLeave = false,
                            EmployeePositionId = 3,
                            EmployeeManagementTier = 3,
                        }
                    }
                };
            case NormalManager:
                return new EmployeeDetailsDto
                {
                    ActiveDirectoryId = Guid.Parse(guidString),
                    EmployeeEmail = "normal_manager@email",
                    EmployeePreferredName = "Manager",
                    EmployeeFirstName = "Normal",
                    EmployeeSurname = "Employee",
                    EmployeeOnLeave = false,
                    EmployeePositionId = 2,
                    EmployeeManagementTier = 4,
                    Managers = new List<IUserInfo>
                    {
                        new EmployeeDetailsDto
                        {
                            EmployeePreferredName = "Manager",
                            EmployeeFirstName = "Manager",
                            EmployeeSurname = "Employee",
                            EmployeeEmail = "ToManager@email",
                            EmployeeManagementTier = 3,
                            EmployeePositionId = 4
                        }
                    },
                    ExecutiveDirectors = new List<IUserInfo>
                    {
                        new EmployeeDetailsDto
                        {
                            EmployeeEmail = "ED@email",
                            EmployeePreferredName = "Tier",
                            EmployeeFirstName = "Tier",
                            EmployeeSurname = "3",
                            EmployeePositionId = 3
                        }
                    }
                };
            case NormalExecutiveDirector:
                return new EmployeeDetailsDto
                {
                    ActiveDirectoryId = Guid.Parse(guidString),
                    EmployeeEmail = "normal_ed@email",
                    EmployeePreferredName = "Exec",
                    EmployeeFirstName = "Executive",
                    EmployeeSurname = "Employee",
                    EmployeeOnLeave = false,
                    EmployeePositionId = 2,
                    EmployeeManagementTier = 3,
                };
            case DualManager:
                return new EmployeeDetailsDto
                {
                    ActiveDirectoryId = Guid.Parse(guidString),
                    EmployeeEmail = "dual_manager_employee@email",
                    EmployeePreferredName = "Dual",
                    EmployeeFirstName = "Dual Manager",
                    EmployeeSurname = "Employee",
                    EmployeeOnLeave = false,
                    Managers = new List<IUserInfo>
                    {
                        new EmployeeDetailsDto
                        {
                            EmployeePreferredName = "Manager 1",
                            EmployeeFirstName = "Manager",
                            EmployeeSurname = "Employee",
                            EmployeeEmail = "ToManager1@email",
                            EmployeeManagementTier = 4,
                            EmployeePositionId = 2,
                            EmployeePositionTitle = "Dual Occupant Position"
                        },
                        new EmployeeDetailsDto
                        {
                            EmployeePreferredName = "Manager 2",
                            EmployeeFirstName = "Manager",
                            EmployeeSurname = "Employee",
                            EmployeeEmail = "ToManager2@email",
                            EmployeeManagementTier = 4,
                            EmployeePositionId = 2,
                            EmployeePositionTitle = "Dual Occupant Position"
                        }
                    },
                    ExecutiveDirectors = new List<IUserInfo>
                    {
                        new EmployeeDetailsDto
                        {
                            EmployeeEmail = "ED@email",
                            EmployeePreferredName = "Tier",
                            EmployeeFirstName = "Tier",
                            EmployeeSurname = "3",
                            EmployeePositionId = 3
                        }
                    }
                };
            case ManagerlessEmployee:
                return new EmployeeDetailsDto
                {
                    ActiveDirectoryId = Guid.Parse(guidString),
                    EmployeeEmail = "managerless_employee@email",
                    EmployeePreferredName = "Test",
                    EmployeeFirstName = "Test",
                    EmployeeSurname = "Employee",
                    EmployeeOnLeave = false,
                    ExecutiveDirectors = new List<IUserInfo>
                    {
                        new EmployeeDetailsDto
                        {
                            ActiveDirectoryId = Guid.Parse(guidString),
                            EmployeeEmail = "normal_ed@email",
                            EmployeePreferredName = "Exec",
                            EmployeeFirstName = "Executive",
                            EmployeeSurname = "Employee",
                            EmployeeOnLeave = false,
                            EmployeePositionId = 3,
                            EmployeeManagementTier = 3
                        }
                    }
                };
            case ManagerlessAndEdlessEmployee:
                return new EmployeeDetailsDto
                {
                    ActiveDirectoryId = Guid.Parse(guidString),
                    EmployeeEmail = "no_manager_or_ed_employee@email",
                    EmployeePreferredName = "Test",
                    EmployeeFirstName = "Test",
                    EmployeeSurname = "Employee",
                    EmployeeOnLeave = false,
                    EmployeePositionId = 4,
                    EmployeeNumber = "number"
                };
            case EdlessEmployee:
                return new EmployeeDetailsDto
                {
                    ActiveDirectoryId = Guid.Parse(guidString),
                    EmployeeEmail = "no_ed_employee@email",
                    EmployeePreferredName = "No ED",
                    EmployeeFirstName = "No Executive Director",
                    EmployeeSurname = "Employee",
                    EmployeeOnLeave = false,
                    EmployeeManagementTier = 9,
                    EmployeePositionId = 5,
                    EmployeeNumber = "number",
                    Managers = new List<IUserInfo>
                    {
                        new EmployeeDetailsDto
                        {
                            EmployeePreferredName = "Manager",
                            EmployeeFirstName = "Manager",
                            EmployeeSurname = "Employee",
                            EmployeeEmail = "ToManager@email",
                            EmployeeManagementTier = 4,
                            EmployeePositionId = 2
                        }
                    }
                };
            case EdPAndC:
                return new EmployeeDetailsDto
                {
                    ActiveDirectoryId = Guid.Parse(guidString),
                    EmployeeEmail = "EdPAndC@email",
                    EmployeePreferredName = "Ed",
                    EmployeeFirstName = "Executive",
                    EmployeeSurname = "Director P&C",
                    Directorate = "People and Culture",
                    EmployeeOnLeave = false,
                    EmployeePositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID,
                    Managers = new List<IUserInfo>
                    {
                        new EmployeeDetailsDto
                        {
                            EmployeePreferredName = "MD",
                            EmployeeFirstName = "Managing",
                            EmployeeSurname = "Director DoT",
                            EmployeeEmail = "MdDot@email",
                            EmployeeManagementTier = 4,
                            EmployeePositionId = ConflictOfInterest.MANAGING_DIRECTOR_DOT
                        }
                    }
                };
            case DirectorGeneral:
                return new EmployeeDetailsDto
                {
                    ActiveDirectoryId = Guid.Parse(guidString),
                    EmployeeEmail = "director_general@email",
                    EmployeePreferredName = "DG",
                    EmployeeFirstName = "Director",
                    EmployeeSurname = "General",
                    EmployeeOnLeave = false,
                    EmployeePositionId = ConflictOfInterest.DIRECTOR_GENERAL_POSITION_NUMBER,
                };
        }

        return null;
    }
}