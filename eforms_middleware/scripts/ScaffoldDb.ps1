dotnet ef dbcontext scaffold "Server=localhost;Database=dot.eforms.database;Trusted_Connection=True;trustServerCertificate=true;" `
--no-onconfiguring `
-p ..\..\DoT.Infrastructure\DoT.Infrastructure.csproj `
-s .\..\eforms_middleware.csproj `
-c AppDbContext `
--context-dir ./DbModels `
--force `
Microsoft.EntityFrameworkCore.SqlServer `
-o DbModels\Entities `
-t Ref_Form_Status -t Form_History -t Form_Info `
-t Task_Info -t Ref_Directorates `
-t Ref_Location -t All_Forms -t DB_Errors `
-t Adf_Positions -t Adf_Users -t Form_Permissions -t Adf_Groups -t Adf_Group_Members `
-t Camp_Rate -t Ref_Location -t Ref_Region -t Salary_Rate -t Travel_Location -t Travel_Rate -t Travel_Time `
-t Workflow_Btn -t Salary_Level `
-t Form_Attachments