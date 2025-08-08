namespace DoT.Infrastructure.DbModels;

public class TrelisReportModelDto
{
    public int TrelisBranchId { get; set; }
    public string Branch { get; set; }
    public string Directorate { get; set; }
    public string EmployeeEmail { get; set; }
    public string EmployeeName { get; set; }
    public string PositionTitle { get; set; }
    public string EmployeeNumber { get; set; }
    public int? PositionId { get; set; }
}