using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using DoT.Infrastructure.DbModels.Entities;

namespace DoT.Infrastructure.DbModels
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AdfGroup> AdfGroups { get; set; }
        public virtual DbSet<AdfGroupMember> AdfGroupMembers { get; set; }
        public virtual DbSet<AdfPosition> AdfPositions { get; set; }
        public virtual DbSet<AdfUser> AdfUsers { get; set; }
        public virtual DbSet<AllForm> AllForms { get; set; }
        public virtual DbSet<CampRate> CampRates { get; set; }
        public virtual DbSet<DbError> DbErrors { get; set; }
        public virtual DbSet<FormAttachment> FormAttachments { get; set; }
        public virtual DbSet<FormHistory> FormHistories { get; set; }
        public virtual DbSet<FormInfo> FormInfos { get; set; }
        public virtual DbSet<FormPermission> FormPermissions { get; set; }
        public virtual DbSet<RefDirectorate> RefDirectorates { get; set; }
        public virtual DbSet<RefFormStatus> RefFormStatuses { get; set; }
        public virtual DbSet<RefLocation> RefLocations { get; set; }
        public virtual DbSet<RefRegion> RefRegions { get; set; }
        public virtual DbSet<SalaryLevel> SalaryLevels { get; set; }
        public virtual DbSet<SalaryRate> SalaryRates { get; set; }
        public virtual DbSet<TaskInfo> TaskInfos { get; set; }
        public virtual DbSet<TravelLocation> TravelLocations { get; set; }
        public virtual DbSet<TravelRate> TravelRates { get; set; }
        public virtual DbSet<TravelTime> TravelTimes { get; set; }
        public virtual DbSet<WorkflowBtn> WorkflowBtns { get; set; }
        public virtual DbSet<EmailSentInfo> EmailSentInfos { get; set; }
        public virtual DbSet<RefGLFunds> RefGLFunds { get; set; }
        public virtual DbSet<RefGLCostCentres> RefGLCostCentres { get; set; }   
        public virtual DbSet<RefGLLocations> RefGLLocations { get; set; }
        public virtual DbSet<RefGLProjects> RefGLProjects { get; set; }
        public virtual DbSet<RefGLActivities> RefGLActivities { get; set; }
        public virtual DbSet<RefGLAccountsSubAccounts> RefGLAccountsSubAccounts { get; set; }
        public virtual DbSet<NCLocation> NcLocations { get; set; }
        public virtual DbSet<RefWASuburb> RefWASuburbs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<AdfGroup>(entity =>
            {
                entity.ToTable("Adf_Groups");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ActiveRecord)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.GroupEmail).IsRequired();

                entity.Property(e => e.GroupEmailNormalized).IsRequired();

                entity.Property(e => e.GroupName).IsRequired();

                entity.Property(e => e.GroupNameNormalized).IsRequired();

                entity.Property(e => e.IngestedAtUtc)
                    .HasColumnType("date")
                    .HasColumnName("Ingested_At_Utc");
            });

            modelBuilder.Entity<AdfGroupMember>(entity =>
            {
                entity.HasKey(e => new { e.MemberId, e.GroupId })
                    .HasName("PK_Adf_Group_Members_Id");

                entity.ToTable("Adf_Group_Members");

                entity.Property(e => e.ActiveRecord)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.IngestedAtUtc)
                    .HasColumnType("date")
                    .HasColumnName("Ingested_At_Utc");

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.AdfGroupMembers)
                    .HasForeignKey(d => d.GroupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Adf_Group_Members_To_Adf_Groups");

                entity.HasOne(d => d.Member)
                    .WithMany(p => p.AdfGroupMembers)
                    .HasForeignKey(d => d.MemberId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Adf_Group_To_Adf_Users");
            });

            modelBuilder.Entity<AdfPosition>(entity =>
            {
                entity.ToTable("Adf_Positions");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ActiveRecord)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Classification).HasMaxLength(100);

                entity.Property(e => e.Directorate)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.IngestedAtUtc)
                    .HasColumnType("date")
                    .HasColumnName("Ingested_At_Utc");

                entity.Property(e => e.ManagementTier).HasColumnName("Management_Tier");

                entity.Property(e => e.OccupancyType)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Occupancy_Type");

                entity.Property(e => e.PositionCreatedDate)
                    .HasColumnType("date")
                    .HasColumnName("Position_Created_Date");

                entity.Property(e => e.PositionNumber)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Position_Number");

                entity.Property(e => e.PositionStatus)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Position_Status");

                entity.Property(e => e.PositionTitle)
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .HasColumnName("Position_Title");

                entity.Property(e => e.ReportsPositionId).HasColumnName("Reports_PositionId");

                entity.Property(e => e.ReportsPositionNumber)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Reports_Position_Number");

                entity.HasOne(d => d.ReportsPosition)
                    .WithMany(p => p.InverseReportsPosition)
                    .HasForeignKey(d => d.ReportsPositionId)
                    .HasConstraintName("FK_Adf_Positions_To_Manager_Position");
            });

            modelBuilder.Entity<AdfUser>(entity =>
            {
                entity.HasKey(e => e.ActiveDirectoryId)
                    .HasName("PK_Adf_Users_AzureObjectId");

                entity.ToTable("Adf_Users");

                entity.Property(e => e.ActiveDirectoryId).ValueGeneratedNever();

                entity.Property(e => e.ActiveRecord)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.EmployeeEmail)
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .HasColumnName("Employee_Email");

                entity.Property(e => e.EmployeeEmailNormalized)
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .HasColumnName("Employee_Email_Normalized");

                entity.Property(e => e.EmployeeFirstName)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Employee_First_Name");

                entity.Property(e => e.EmployeeGender)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Employee_Gender");

                entity.Property(e => e.EmployeeNameNormalized)
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .HasColumnName("Employee_Name_Normalized");

                entity.Property(e => e.EmployeeNumber)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Employee_Number");

                entity.Property(e => e.EmployeePreferredName)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Employee_Preferred_Name");

                entity.Property(e => e.EmployeePreferredNameNormalized)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Employee_Preferred_Name_Normalized");

                entity.Property(e => e.EmployeeSecondName)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Employee_Second_Name");

                entity.Property(e => e.EmployeeStartDate)
                    .HasColumnType("date")
                    .HasColumnName("Employee_Start_Date");

                entity.Property(e => e.EmployeeSurname)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Employee_Surname");

                entity.Property(e => e.EmployeeTerminationDate)
                    .HasColumnType("date")
                    .HasColumnName("Employee_Termination_Date");

                entity.Property(e => e.EmployeeTitle)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Employee_Title");

                entity.Property(e => e.IngestedAtUtc)
                    .HasColumnType("date")
                    .HasColumnName("Ingested_At_Utc");

                entity.Property(e => e.LeaveFlag)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("Leave_Flag");

                entity.Property(e => e.ReportsToPositionId).HasColumnName("Reports_To_PositionId");

                entity.Property(e => e.UserId)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("User_Id");

                entity.HasOne(d => d.Position)
                    .WithMany(p => p.AdfUserPositions)
                    .HasForeignKey(d => d.PositionId)
                    .HasConstraintName("FK_Adf_User_To_Adf_Position");

                entity.HasOne(d => d.ReportsToPosition)
                    .WithMany(p => p.AdfUserReportsToPositions)
                    .HasForeignKey(d => d.ReportsToPositionId)
                    .HasConstraintName("FK_Adf_Contractor_Reports_To_Adf_Position");
            });

            modelBuilder.Entity<AllForm>(entity =>
            {
                entity.HasKey(e => e.AllFormsId)
                    .HasName("PK__All_Form__BCC7AD326C2D3619");

                entity.ToTable("All_Forms");

                entity.Property(e => e.AllFormsId).HasColumnName("AllFormsID");

                entity.Property(e => e.ActiveRecord).HasDefaultValueSql("((1))");

                entity.Property(e => e.ArchiveUrl).HasColumnName("ArchiveURL");

                entity.Property(e => e.BusinessDirectorateId).HasColumnName("BusinessDirectorateID");

                entity.Property(e => e.BusinessOwner).HasMaxLength(100);

                entity.Property(e => e.Created).HasColumnType("datetime");

                entity.Property(e => e.CreatedBy).HasMaxLength(100);

                entity.Property(e => e.FormCategory).HasMaxLength(50);

                entity.Property(e => e.FormCode).HasMaxLength(5);

                entity.Property(e => e.FormGroupName).HasMaxLength(100);

                entity.Property(e => e.FormId)
                    .IsRequired()
                    .HasColumnName("FormID");

                entity.Property(e => e.FormType).HasMaxLength(50);

                entity.Property(e => e.FormUrl).HasColumnName("FormURL");

                entity.Property(e => e.IconValue).HasMaxLength(100);

                entity.Property(e => e.IsEmployeeOnly)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Modified).HasColumnType("datetime");

                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                entity.Property(e => e.ParentAllFormsId).HasColumnName("ParentAllFormsID");

                entity.Property(e => e.RequestType).HasMaxLength(50);

                entity.Property(e => e.VisibleTo).HasMaxLength(50);

                entity.HasOne(d => d.ParentAllForms)
                    .WithMany(p => p.InverseParentAllForms)
                    .HasForeignKey(d => d.ParentAllFormsId)
                    .HasConstraintName("FK_All_Forms_To_All_Forms_Parent");
            });

            modelBuilder.Entity<CampRate>(entity =>
            {
                entity.ToTable("Camp_Rate");

                entity.Property(e => e.CampId)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.ParallelType).HasMaxLength(50);

                entity.Property(e => e.Rate).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Type).HasMaxLength(50);
            });

            modelBuilder.Entity<DbError>(entity =>
            {
                entity.HasKey(e => e.ErrorId)
                    .HasName("PK__Db_Error");

                entity.ToTable("DB_Errors");

                entity.Property(e => e.ErrorId).HasColumnName("ErrorID");

                entity.Property(e => e.ErrorDateTime).HasColumnType("datetime");

                entity.Property(e => e.ErrorMessage).IsUnicode(false);

                entity.Property(e => e.ErrorProcedure).IsUnicode(false);

                entity.Property(e => e.UserName)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<FormAttachment>(entity =>
            {
                entity.ToTable("Form_Attachments");

                entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");

                entity.Property(e => e.Created).HasColumnType("datetime");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.FileType)
                    .IsRequired()
                    .HasMaxLength(5);

                entity.Property(e => e.Modified).HasColumnType("datetime");

                entity.Property(e => e.ModifiedBy)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasOne(d => d.Form)
                    .WithMany(p => p.FormAttachments)
                    .HasForeignKey(d => d.FormId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Form_Attachments_To_Form_Info");

                entity.HasOne(d => d.Permission)
                    .WithMany(p => p.FormAttachments)
                    .HasForeignKey(d => d.PermissionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Form_Attachments_To_Form_Permission");
            });

            modelBuilder.Entity<FormHistory>(entity =>
            {
                entity.ToTable("Form_History");

                entity.Property(e => e.FormHistoryId).HasColumnName("FormHistoryID");

                entity.Property(e => e.ActionBy).HasMaxLength(500);

                entity.Property(e => e.ActionByPosition).HasMaxLength(500);

                entity.Property(e => e.ActionType).HasMaxLength(200);

                entity.Property(e => e.ActiveRecord).HasDefaultValueSql("((1))");

                entity.Property(e => e.AllFormsId).HasColumnName("AllFormsID");

                entity.Property(e => e.Created).HasColumnType("datetime");

                entity.Property(e => e.FormInfoId).HasColumnName("FormInfoID");

                entity.Property(e => e.FormStatusId).HasColumnName("FormStatusID");

                entity.Property(e => e.RejectedReason).HasMaxLength(1000);
            });

            modelBuilder.Entity<FormInfo>(entity =>
            {
                entity.ToTable("Form_Info");

                entity.Property(e => e.FormInfoId).HasColumnName("FormInfoID");

                entity.Property(e => e.ActiveRecord).HasDefaultValueSql("((1))");

                entity.Property(e => e.AllFormsId).HasColumnName("AllFormsID");

                entity.Property(e => e.ChildFormType).HasMaxLength(250);

                entity.Property(e => e.CompletedDate).HasColumnType("datetime");

                entity.Property(e => e.Created).HasColumnType("datetime");

                entity.Property(e => e.CreatedBy).HasMaxLength(500);

                entity.Property(e => e.FormOwnerDirectorate).HasMaxLength(250);

                entity.Property(e => e.FormOwnerEmail).HasMaxLength(500);

                entity.Property(e => e.FormOwnerEmployeeNo).HasMaxLength(50);

                entity.Property(e => e.FormOwnerName).HasMaxLength(50);

                entity.Property(e => e.FormOwnerPositionNo).HasMaxLength(50);

                entity.Property(e => e.FormOwnerPositionTitle).HasMaxLength(500);

                entity.Property(e => e.FormStatusId).HasColumnName("FormStatusID");

                entity.Property(e => e.FormSubStatus).HasMaxLength(50);

                entity.Property(e => e.InitiatedForDirectorate).HasMaxLength(250);

                entity.Property(e => e.InitiatedForEmail).HasMaxLength(500);

                entity.Property(e => e.InitiatedForName).HasMaxLength(50);

                entity.Property(e => e.Modified).HasColumnType("datetime");

                entity.Property(e => e.ModifiedBy).HasMaxLength(500);

                entity.Property(e => e.NextApprovalLevel).HasMaxLength(250);

                entity.Property(e => e.Pdfguid).HasColumnName("PDFGuid");

                entity.Property(e => e.Response).IsRequired();

                entity.Property(e => e.SubmittedDate).HasColumnType("datetime");

                entity.HasOne(d => d.AllForms)
                    .WithMany(p => p.FormInfos)
                    .HasForeignKey(d => d.AllFormsId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Form_Info_To_All_Forms");
            });

            modelBuilder.Entity<FormPermission>(entity =>
            {
                entity.ToTable("Form_Permissions");

                entity.HasOne(d => d.Form)
                    .WithMany(p => p.FormPermissions)
                    .HasForeignKey(d => d.FormId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Form_Permission_To_Form_Info");

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.FormPermissions)
                    .HasForeignKey(d => d.GroupId)
                    .HasConstraintName("FK_Form_Permission_To_Adf_Groups");

                entity.HasOne(d => d.Position)
                    .WithMany(p => p.FormPermissions)
                    .HasForeignKey(d => d.PositionId)
                    .HasConstraintName("FK_Form_Permission_To_Adf_Positions");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.FormPermissions)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Form_Permission_To_Adf_Users");
            });

            modelBuilder.Entity<RefDirectorate>(entity =>
            {
                entity.HasKey(e => e.RefDirectoratesId)
                    .HasName("PK__Ref_Dire__0F0149D43923C281");

                entity.ToTable("Ref_Directorates");

                entity.Property(e => e.RefDirectoratesId).HasColumnName("RefDirectoratesID");

                entity.Property(e => e.ActiveRecord).HasDefaultValueSql("((1))");

                entity.Property(e => e.Directorate).HasMaxLength(250);
            });

            modelBuilder.Entity<RefFormStatus>(entity =>
            {
                entity.HasKey(e => e.RefStatusesId)
                    .HasName("PK_Form_Status");

                entity.ToTable("Ref_Form_Status");

                entity.Property(e => e.RefStatusesId).HasColumnName("RefStatusesID");

                entity.Property(e => e.ActiveRecord).HasDefaultValueSql("((1))");

                entity.Property(e => e.Status).HasMaxLength(500);
            });

            modelBuilder.Entity<RefLocation>(entity =>
            {
                entity.ToTable("Ref_Location");

                entity.Property(e => e.RefLocationId).HasColumnName("RefLocationID");

                entity.Property(e => e.ActiveRecord).HasDefaultValueSql("((1))");

                entity.Property(e => e.City).HasMaxLength(100);

                entity.Property(e => e.LocationTitle).HasMaxLength(500);

                entity.Property(e => e.OfficeLocationName).HasMaxLength(250);

                entity.Property(e => e.PostCode).HasMaxLength(10);

                entity.Property(e => e.State).HasMaxLength(5);

                entity.Property(e => e.Street).HasMaxLength(250);
            });

            modelBuilder.Entity<RefRegion>(entity =>
            {
                entity.ToTable("Ref_Region");

                entity.Property(e => e.ParallelType).HasMaxLength(100);

                entity.Property(e => e.RegionalArea).HasMaxLength(100);

                entity.Property(e => e.RegionalOpsPositionNumber).HasMaxLength(100);

                entity.Property(e => e.RegionalOpsPositionTitle).HasMaxLength(100);
            });

            modelBuilder.Entity<SalaryLevel>(entity =>
            {
                entity.ToTable("Salary_Level");

                entity.Property(e => e.GrossSalary).HasColumnType("decimal(18, 4)");

                entity.Property(e => e.PositionLevel)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.PositionStep).HasMaxLength(100);
            });

            modelBuilder.Entity<SalaryRate>(entity =>
            {
                entity.ToTable("Salary_Rate");

                entity.Property(e => e.From).HasColumnType("decimal(14, 2)");

                entity.Property(e => e.Rate).HasMaxLength(100);

                entity.Property(e => e.To).HasColumnType("decimal(14, 2)");
            });

            modelBuilder.Entity<TaskInfo>(entity =>
            {
                entity.ToTable("Task_Info");

                entity.Property(e => e.TaskInfoId).HasColumnName("TaskInfoID");

                entity.Property(e => e.ActiveRecord).HasDefaultValueSql("((1))");

                entity.Property(e => e.AllFormsId).HasColumnName("AllFormsID");

                entity.Property(e => e.AssignedTo).HasMaxLength(500);

                entity.Property(e => e.EmailInfoId).HasColumnName("EmailInfoID");

                entity.Property(e => e.Escalation).HasDefaultValueSql("((0))");

                entity.Property(e => e.EscalationDate).HasColumnType("datetime");

                entity.Property(e => e.FormInfoId).HasColumnName("FormInfoID");

                entity.Property(e => e.FormOwnerEmail).HasMaxLength(500);

                entity.Property(e => e.ReminderTo).HasMaxLength(500);

                entity.Property(e => e.SpecialReminderDate).HasColumnType("datetime");

                entity.Property(e => e.SpecialReminderTo).HasMaxLength(500);

                entity.Property(e => e.TaskCompletedBy).HasMaxLength(500);

                entity.Property(e => e.TaskCompletedDate).HasColumnType("datetime");

                entity.Property(e => e.TaskCreatedBy).HasMaxLength(500);

                entity.Property(e => e.TaskCreatedDate).HasColumnType("datetime");

                entity.Property(e => e.TaskStatus).HasMaxLength(50);

                entity.HasOne(d => d.AllForms)
                    .WithMany(p => p.TaskInfos)
                    .HasForeignKey(d => d.AllFormsId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Task_Info_All_Forms");

                entity.HasOne(d => d.FormInfo)
                    .WithMany(p => p.TaskInfos)
                    .HasForeignKey(d => d.FormInfoId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Task_Info_Form_Info");
            });

            modelBuilder.Entity<TravelLocation>(entity =>
            {
                entity.ToTable("Travel_Location");

                entity.Property(e => e.Location).HasMaxLength(100);

                entity.Property(e => e.RefRegionalId).HasColumnName("Ref_Regional_Id");

                entity.HasOne(d => d.RefRegional)
                    .WithMany(p => p.TravelLocations)
                    .HasForeignKey(d => d.RefRegionalId)
                    .HasConstraintName("FK_Travel_Location_Ref_Region");
            });

            modelBuilder.Entity<TravelRate>(entity =>
            {
                entity.ToTable("Travel_Rate");

                entity.Property(e => e.Accomodation).HasColumnType("decimal(14, 2)");

                entity.Property(e => e.ActiveDate).HasColumnType("date");

                entity.Property(e => e.Breakfast).HasColumnType("decimal(14, 2)");

                entity.Property(e => e.Dinner).HasColumnType("decimal(14, 2)");

                entity.Property(e => e.Food).HasColumnType("decimal(14, 2)");

                entity.Property(e => e.Incidentals).HasColumnType("decimal(14, 2)");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Lunch).HasColumnType("decimal(14, 2)");

                entity.Property(e => e.RateType).HasMaxLength(100);

                entity.Property(e => e.SalaryRateId).HasColumnName("Salary_Rate_Id");

                entity.Property(e => e.Total).HasColumnType("decimal(14, 2)");

                entity.Property(e => e.TravelLocationId).HasColumnName("Travel_Location_Id");

                entity.HasOne(d => d.SalaryRate)
                    .WithMany(p => p.TravelRates)
                    .HasForeignKey(d => d.SalaryRateId)
                    .HasConstraintName("FK_Travel_Rate_Salary_Rate");

                entity.HasOne(d => d.TravelLocation)
                    .WithMany(p => p.TravelRates)
                    .HasForeignKey(d => d.TravelLocationId)
                    .HasConstraintName("FK_Travel_Rate_Travel_Location");
            });

            modelBuilder.Entity<TravelTime>(entity =>
            {
                entity.ToTable("Travel_Time");

                entity.Property(e => e.Rate).HasColumnType("decimal(14, 4)");

                entity.Property(e => e.TimeInterval).HasMaxLength(100);
            });

            modelBuilder.Entity<EmailSentInfo>(entity =>
            {
                entity.ToTable("Email_Sent_Info");

                entity.Property(e => e.EmailSentInfoId).HasColumnName("EmailSentInfoID");
                entity.Property(e => e.AllFormsId).HasColumnName("AllFormsID");
                entity.Property(e => e.FormInfoId).HasColumnName("FormInfoID");
                entity.Property(e => e.EmailInfoId).HasColumnName("EmailInfoID");
                entity.Property(e => e.EmailSubject).HasColumnName("EmailSubject");
                entity.Property(e => e.EmailFrom).HasColumnName("EmailFrom");
                entity.Property(e => e.EmailTo).HasColumnName("EmailTo");
                entity.Property(e => e.EmailCc).HasColumnName("EmailCc");
                entity.Property(e => e.EmailBcc).HasColumnName("EmailBCC");
                entity.Property(e => e.EmailContent).HasColumnName("EmailContent");
                entity.Property(e => e.SentOn).HasColumnName("SentOn");
                entity.Property(e => e.EmailSentFlag).HasColumnName("EmailSentFlag"); 
                entity.Property(e => e.ActiveRecord).HasColumnName("ActiveRecord");

                entity.HasOne(d => d.AllForms)
                    .WithMany(p => p.EmailSentInfos)
                    .HasForeignKey(d => d.AllFormsId)
                    .HasConstraintName("FK_Email_Sent_Info_All_Forms");
            });

            modelBuilder.Entity<WorkflowBtn>(entity =>
            {
                entity.ToTable("Workflow_Btn");

                entity.Property(e => e.BtnText).HasMaxLength(50);

                entity.Property(e => e.FormSubStatus).HasMaxLength(50);

                entity.HasOne(d => d.Form)
                    .WithMany(p => p.WorkflowBtns)
                    .HasForeignKey(d => d.FormId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Workflow_Btn_Form_Info");

                entity.HasOne(d => d.Permision)
                    .WithMany(p => p.WorkflowBtns)
                    .HasForeignKey(d => d.PermisionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Workflow_Btn_Form_Permissions");
            });

            modelBuilder.Entity<RefGLFunds>(entity =>
            {
                entity.ToTable("Ref_GL_Funds");
                entity.Property(e => e.RefGLFundsID).HasColumnName("RefGLFundsID");
                entity.Property(e => e.Funds).HasColumnName("Funds");
                entity.Property(e => e.ActiveRecord).HasColumnName("ActiveRecord");
                entity.Property(e => e.Name).HasColumnName("Name");
            });

            modelBuilder.Entity<RefGLCostCentres>(entity =>
            {
                entity.ToTable("Ref_GL_Cost_Centres");
                entity.Property(e => e.RefGLCostCentresID).HasColumnName("RefGLCostCentresID");
                entity.Property(e => e.CostCentre).HasColumnName("CostCentre");
                entity.Property(e => e.ActiveRecord).HasColumnName("ActiveRecord");
                entity.Property(e => e.Name).HasColumnName("Name");
            });

            modelBuilder.Entity<RefGLLocations>(entity =>
            {
                entity.ToTable("Ref_GL_Locations");
                entity.Property(e => e.RefGLLocationsID).HasColumnName("RefGLLocationsID");
                entity.Property(e => e.Locations).HasColumnName("Locations");
                entity.Property(e => e.ActiveRecord).HasColumnName("ActiveRecord");
                entity.Property(e => e.Name).HasColumnName("Name");
            });
            
            modelBuilder.Entity<RefGLActivities>(entity =>
            {
                entity.ToTable("Ref_GL_Activities");
                entity.Property(e => e.RefGLActivitiesID).HasColumnName("RefGLActivitiesID");
                entity.Property(e => e.Activities).HasColumnName("Activities");
                entity.Property(e => e.ActiveRecord).HasColumnName("ActiveRecord");
                entity.Property(e => e.Name).HasColumnName("Name");
            });

            modelBuilder.Entity<RefGLProjects>(entity =>
            {
                entity.ToTable("Ref_GL_Projects");
                entity.Property(e => e.RefGLProjectsID).HasColumnName("RefGLProjectsID");
                entity.Property(e => e.Projects).HasColumnName("Projects");
                entity.Property(e => e.ActiveRecord).HasColumnName("ActiveRecord");
                entity.Property(e => e.Name).HasColumnName("Name");
            });

            modelBuilder.Entity<RefGLAccountsSubAccounts>(entity =>
            {
                entity.ToTable("Ref_GL_Accounts_Sub_Accounts");
                entity.Property(e => e.RefGLAccountsSubAccountsID).HasColumnName("RefGLAccountsSubAccountsID");
                entity.Property(e => e.AccountSubAccounts).HasColumnName("Account_Sub_Accounts");
                entity.Property(e => e.ActiveRecord).HasColumnName("ActiveRecord");
                entity.Property(e => e.Name).HasColumnName("Name");
            });

            modelBuilder.Entity<NCLocation>(entity =>
            {
                entity.ToTable("NC_Location");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Location).HasColumnName("Location").IsRequired();
            });

            modelBuilder.Entity<RefWASuburb>(entity =>
            {
                entity.ToTable("Ref_WASuburbs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.SuburbName).HasColumnName("SuburbName").IsRequired();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
