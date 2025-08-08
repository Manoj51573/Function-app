using System.Collections.Generic;
using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.DataModel;

public class MigrationModel
{
    public int FormId { get; set; }
    public List<string> Attachments { get; set; }
    public COIMain Response { get; set; }
    public int PermissionId { get; set; }
}