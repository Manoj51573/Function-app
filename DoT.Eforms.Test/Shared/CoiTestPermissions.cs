using System;
using System.Collections.Generic;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;

namespace DoT.Eforms.Test.Shared;

public static class CoiTestPermissions
{
    public static List<FormPermission> ManagerlessOwnerView => new()
    {
        new()
        {
            IsOwner = true, UserId = Guid.Parse(CoiTestEmployees.ManagerlessEmployee),
            User = new AdfUser
                { EmployeeEmail = "managereless@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee" },
            PermissionFlag = (byte)PermissionFlag.View
        }
    };
    
    public static List<FormPermission> EdlessOwnerViewManagerAction => new()
    {
        new()
        {
            IsOwner = true, UserId = new Guid(CoiTestEmployees.EdlessEmployee),
            User = new AdfUser
                { EmployeeEmail = "edless@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee" },
            PermissionFlag = (byte)PermissionFlag.View
        },
        new()
        {
            PositionId = 1,
            Position = new AdfPosition
            {
                ManagementTier = 9,
                AdfUserPositions = new List<AdfUser>
                {
                    new ()
                    {
                        EmployeeEmail = "ToManager@email", EmployeePreferredName = "Manager", EmployeeSurname = "Employee"
                    }
                }
            },
            PermissionFlag = (byte)PermissionFlag.UserActionable
        }
    };
    
    public static List<FormPermission> OwnerView => new()
    {
        new()
        {
            IsOwner = true, UserId = Guid.Parse(CoiTestEmployees.NormalEmployee),
            User = new AdfUser
                { EmployeeEmail = "employee@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee" },
            PermissionFlag = (byte)PermissionFlag.View
        },
        new()
        {
            UserId = null, PositionId = 2,
            PermissionFlag = (byte)PermissionFlag.View
        }
    };
    
    public static List<FormPermission> OwnerViewManagerAction => new()
    {
        new()
        {
            IsOwner = true, UserId = Guid.Parse(CoiTestEmployees.NormalEmployee),
            User = new AdfUser
                { EmployeeEmail = "employee@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee" },
            PermissionFlag = (byte)PermissionFlag.View
        },
        new()
        {
            PositionId = 2,
            Position = new AdfPosition
            {
                ManagementTier = 9,
                AdfUserPositions = new List<AdfUser>
                {
                    new ()
                    {
                        EmployeeEmail = "ToManager@email", EmployeePreferredName = "Manager", EmployeeSurname = "Employee"
                    }
                }
            },
            PermissionFlag = (byte)PermissionFlag.UserActionable
        }
    };
    
    public static List<FormPermission> OwnerViewEdAction => new()
    {
        new()
        {
            IsOwner = true, UserId = Guid.Parse(CoiTestEmployees.ManagerlessEmployee),
            User = new AdfUser
                { EmployeeEmail = "employee@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee" },
            PermissionFlag = (byte)PermissionFlag.View
        },
        new()
        {
            PositionId = 3,
            Position = new AdfPosition
            {
                ManagementTier = 3,
                AdfUserPositions = new List<AdfUser>
                {
                    new AdfUser
                    {
                        EmployeeEmail = "ED@email", EmployeePreferredName = "Tier", EmployeeSurname = "3"
                    }
                }
            },
            PermissionFlag = (byte)PermissionFlag.UserActionable
        }
    };
    
    public static List<FormPermission> ManagerlessOwnerViewEdAction => new()
    {
        new()
        {
            IsOwner = true, UserId = Guid.Parse(CoiTestEmployees.ManagerlessEmployee),
            User = new AdfUser
                { EmployeeEmail = "managereless@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee" },
            PermissionFlag = (byte)PermissionFlag.View
        },
        new()
        {
            PositionId = 3,
            Position = new AdfPosition
            {
                ManagementTier = 3,
                AdfUserPositions = new List<AdfUser>
                {
                    new AdfUser
                    {
                        EmployeeEmail = "ED@email", EmployeePreferredName = "Tier", EmployeeSurname = "3"
                    }
                }
            },
            PermissionFlag = (byte)PermissionFlag.UserActionable
        }
    };
    
    public static List<FormPermission> OwnerViewManagerEdAction => new()
    {
        new()
        {
            IsOwner = true, UserId = Guid.Parse(CoiTestEmployees.NormalEmployee),
            User = new AdfUser
                { EmployeeEmail = "tier3ismanager@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee" },
            PermissionFlag = (byte)PermissionFlag.View
        },
        new () { PositionId = 2},
        new()
        {
            PositionId = 3,
            Position = new AdfPosition
            {
                ManagementTier = 3,
                AdfUserPositions = new List<AdfUser>
                {
                    new AdfUser
                    {
                        EmployeeEmail = "ED@email", EmployeePreferredName = "Tier", EmployeeSurname = "3"
                    }
                }
            },
            PermissionFlag = (byte)PermissionFlag.UserActionable
        }
    };
    
    public static List<FormPermission> OwnerManagerViewEdPAndCAction => new()
    {
        new()
        {
            IsOwner = true, UserId = Guid.Parse(CoiTestEmployees.NormalEmployee),
            User = new AdfUser
                { EmployeeEmail = "tier3ismanager@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee" },
            //Position = new AdfPosition { Manag},
            PermissionFlag = (byte)PermissionFlag.View
        },
        // manager
        new FormPermission((byte)PermissionFlag.View, positionId: 2),
        new()
        {
            PositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID,
            Position = new AdfPosition
            {
                ManagementTier = 3,
                AdfUserPositions = new List<AdfUser>
                {
                    new AdfUser
                    {
                        EmployeeEmail = "edp&c@email", EmployeePreferredName = "ED", EmployeeSurname = "P&C"
                    }
                }
            },
            PermissionFlag = (byte)PermissionFlag.UserActionable
        }
    };
    
    public static List<FormPermission> OwnerViewEdPAndCAction => new()
    {
        new()
        {
            IsOwner = true, UserId = Guid.Parse(CoiTestEmployees.NormalEmployee),
            User = new AdfUser
                { EmployeeEmail = "tier3ismanager@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee" },
            //Position = new AdfPosition { Manag},
            PermissionFlag = (byte)PermissionFlag.View
        },
        new()
        {
            PositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID,
            Position = new AdfPosition
            {
                ManagementTier = 3,
                AdfUserPositions = new List<AdfUser>
                {
                    new AdfUser
                    {
                        EmployeeEmail = "edp&c@email", EmployeePreferredName = "ED", EmployeeSurname = "P&C"
                    }
                }
            },
            PermissionFlag = (byte)PermissionFlag.UserActionable
        }
    };
    
    public static List<FormPermission> ManagerlessOwnerViewEdPAndCAction => new()
    {
        new()
        {
            IsOwner = true, UserId = Guid.Parse(CoiTestEmployees.ManagerlessEmployee),
            User = new AdfUser
                { EmployeeEmail = "managereless@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee" },
            //Position = new AdfPosition { Manag},
            PermissionFlag = (byte)PermissionFlag.View
        },
        new()
        {
            PositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID,
            Position = new AdfPosition
            {
                ManagementTier = 3,
                AdfUserPositions = new List<AdfUser>
                {
                    new AdfUser
                    {
                        EmployeeEmail = "edp&c@email", EmployeePreferredName = "ED", EmployeeSurname = "P&C"
                    }
                }
            },
            PermissionFlag = (byte)PermissionFlag.UserActionable
        }
    };
    
    public static List<FormPermission> OwnerActionManagerViewEdView => new()
    {
        new()
        {
            IsOwner = true, UserId = new Guid(CoiTestEmployees.NormalEmployee),
            User = new AdfUser
            {
                EmployeeEmail = "employee@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee"
            },
            PermissionFlag = (byte)PermissionFlag.UserActionable
        },
        new()
        {
            PositionId = 2,
            Position = new AdfPosition
            {
                ManagementTier = 9,
                AdfUserPositions = new List<AdfUser>
                {
                    new AdfUser
                    {
                        EmployeeEmail = "ToManager@email", EmployeePreferredName = "Manager", EmployeeSurname = "Employee"
                    }
                }
            },
            PermissionFlag = (byte)PermissionFlag.View
        },
        new()
        {
            PositionId = 3,
            Position = new AdfPosition
            {
                ManagementTier = 3,
                AdfUserPositions = new List<AdfUser>
                {
                    new AdfUser
                    {
                        EmployeeEmail = "ED@email", EmployeePreferredName = "Employee", EmployeeSurname = "ED"
                    }
                }
            },
            PermissionFlag = (byte)PermissionFlag.View
        }
    };

    public static List<FormPermission> OwnerManagerChanged => new()
    {
        new()
        {
            IsOwner = true, UserId = new Guid(CoiTestEmployees.NormalEmployee),
            User = new AdfUser
            {
                EmployeeEmail = "employee@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee"
            },
            PermissionFlag = (byte)PermissionFlag.View
        },
        new()
        {
            PermissionFlag = (byte)PermissionFlag.View, PositionId = 9
        }
    };

    public static List<FormPermission> OwnerWithEdLevelManager => new()
    {
        new () { IsOwner = true, },//UserId = Guid.Parse(CoiTestEmployees.EmployeeWithEdManager)},
        new () { PositionId = 3 }
    };

    public static List<FormPermission> OwnerAndManagerViewEdAction => new()
    {
        new () 
        {
            IsOwner = true, UserId = new Guid(CoiTestEmployees.NormalEmployee),
            User = new AdfUser
            {
                EmployeeEmail = "employee@email", EmployeePreferredName = "Test", EmployeeSurname = "Employee"
            },PermissionFlag = (byte)PermissionFlag.View
        },
        new () 
        {
            IsOwner = false, UserId = new Guid(CoiTestEmployees.NormalManager), PositionId = 2,
            User = new AdfUser
            {
                EmployeeEmail = "ToManager@email", EmployeePreferredName = "Manager", EmployeeSurname = "Employee"
            },PermissionFlag = (byte)PermissionFlag.View
        },
        new () 
        {
            IsOwner = false, UserId = new Guid(CoiTestEmployees.NormalExecutiveDirector), PositionId = 3,
            User = new AdfUser
            {
                EmployeeEmail = "ED@email", EmployeePreferredName = "Executive", EmployeeSurname = "Employee"
            },PermissionFlag = (byte)PermissionFlag.UserActionable
        },
    };
}