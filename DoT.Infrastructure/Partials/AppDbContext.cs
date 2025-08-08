using Microsoft.EntityFrameworkCore;

namespace DoT.Infrastructure.DbModels
{
    public partial class AppDbContext
    {
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsersFormStoredProcedureDto>().HasNoKey();
            modelBuilder.Entity<EmployeeDetailsDto>().HasNoKey();
            modelBuilder.Entity<TrelisReportModelDto>().HasNoKey();
        }
    }
}