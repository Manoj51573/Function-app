using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.GetMasterData;
using eforms_middleware.Interfaces;
using eforms_middleware.MessageBuilders;
using eforms_middleware.Services;
using eforms_middleware.Settings;
using eforms_middleware.Validators;
using eforms_middleware.Workflows;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Net.Mail;

[assembly: FunctionsStartup(typeof(eforms_middleware.Startup))]

namespace eforms_middleware
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
            builder.Services.AddMvcCore().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateTimeZoneHandling = DateTimeZoneHandling.Local
            };
            builder.Services.AddDbContext<AppDbContext>(o =>
            {
                SqlConnection connection = new SqlConnection();
                connection.ConnectionString = configuration.GetConnectionString("AZURESQL-ConnectionString");
                var tokenProvider = new AzureServiceTokenProvider();
                connection.AccessToken = tokenProvider.GetAccessTokenAsync("https://database.windows.net").Result;
                o.UseSqlServer(connection);
            });
            builder.Services.AddOptions<BlobOptions>().Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("Blob").Bind(settings);
            });
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddScoped<EmployeeDetailsService>();
            builder.Services.AddScoped<IFormHistoryService, FormHistoryService>();
            builder.Services.AddScoped<E29FormService>();
            builder.Services.AddScoped(typeof(CoIFormService<>));
            builder.Services.AddValidatorsFromAssemblyContaining<CprEmployeeFormValidator>();
            builder.Services.AddScoped<IFormEmailService, FormEmailService>();
            builder.Services.AddScoped<IFormInfoService, FormInfoService>();
            builder.Services.AddScoped<COIApprovalService>();
            builder.Services.AddScoped<BlobService>();
            builder.Services.AddScoped<GBHService>();
            builder.Services.AddScoped<RecruitmentFormService>();
            builder.Services.AddScoped<SecondaryEmploymentService>();
            builder.Services.AddScoped<BaseApprovalService>();
            builder.Services.AddScoped<COINotificationService>();
            builder.Services.AddScoped<COIPermisionService>();
            builder.Services.AddScoped<TravelProposalApprovalService>();
            builder.Services.AddScoped<OnlinePublishingRequestApprovalService>();
            builder.Services.AddScoped<AllowanceClaimsApprovalService>();
            builder.Services.AddScoped<LeaveCashOutApprovalService>();
            builder.Services.AddScoped<LeaveAmendmentFormService>();
            builder.Services.AddScoped<LeaveAmendmentMessageBuilder>();
            builder.Services.AddScoped<MaternityLeaveMessageBuilder>();
            builder.Services.AddScoped<MaternityLeaveFormService>();
            builder.Services.AddScoped<PurchaseLeaveFormService>();
            builder.Services.AddScoped<PurchasedLeaveMessageBuilder>();
            builder.Services.AddScoped<LeaveCashOutFormService>();
            builder.Services.AddScoped<LeaveCashOutMessageBuilder>();
            builder.Services.AddScoped<LeaveWithoutPayFormService>();
            builder.Services.AddScoped<LeaveWithoutPayMessageBuilder>();
            builder.Services.AddScoped<ProRataLeaveFormService>();
            builder.Services.AddScoped<ProRataLeaveMessageBuilder>();
            builder.Services.AddScoped<WorkflowBtnManager>();
            builder.Services.AddScoped<RosterChangeApprovalService>();
            builder.Services.AddScoped<RosterChangeRequestService>();
            builder.Services.AddScoped<RosterChangeMessageBuilder>();
            builder.Services.AddScoped<WHSIncidentReportApprovalService>();
            builder.Services.AddScoped<RefWASuburbsService>();
            

            builder.Services.AddScoped<Func<FormType, FormServiceBase>>(c =>
            {
                return (formType) =>
                {
                    return formType switch
                    {
                        FormType.e29 => c.GetRequiredService<E29FormService>(),
                        FormType.CoI_CPR => c.GetRequiredService<CoIFormService<ClosePersonalRelationship>>(),
                        FormType.CoI_GBC => c.GetRequiredService<CoIFormService<BoardCommitteePAndC>>(),
                        FormType.CoI_Other => c.GetRequiredService<CoIFormService<CoIOther>>(),
                        _ => throw new KeyNotFoundException()
                    };
                };
            });
            builder.Services.AddScoped(typeof(IFormSubtypeHelper<ClosePersonalRelationship>), typeof(CprFormSubtypeHelper));
            builder.Services.AddScoped(typeof(IFormSubtypeHelper<BoardCommitteePAndC>), typeof(GbcFormSubtypeHelper));
            builder.Services.AddScoped(typeof(IFormSubtypeHelper<CoIOther>), typeof(CoIOtherSubtypeHelper));
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped(typeof(ISpViewRepository<>), typeof(SpViewRepository<>));
            builder.Services.AddScoped<IMessageFactoryService, MessageFactoryService>();
            builder.Services.AddScoped<CprMessageBuilder>();
            builder.Services.AddScoped<E29MessageBuilder>();
            builder.Services.AddScoped<GbcMessageBuilder>();
            builder.Services.AddScoped<CoIOtherMessageBuilder>();
            builder.Services.AddScoped<TravelMessageBuilder>();
            builder.Services.AddScoped<AdditionalHoursClaimsMessageBuilder>();
            builder.Services.AddScoped<CasualTimesheetsMessageBuilder>();
            builder.Services.AddScoped<MotorVehicleAllowanceClaimsMessageBuilder>();
            builder.Services.AddScoped<OtherAllowanceClaimsMessageBuilder>();
            builder.Services.AddScoped<OutofHoursContactClaimsMessageBuilder>();
            builder.Services.AddScoped<OvertimeClaimsMessageBuilder>();
            builder.Services.AddScoped<PenaltyShiftAllowanceClaimsMessageBuilder>();
            builder.Services.AddScoped<SeaGoingAllowanceClaimsMessageBuilder>();            
            builder.Services.AddScoped<IEmployeeService, EmployeeService>();
            builder.Services.AddScoped((x) => new SmtpClient(Helper.SMTPHost));
            builder.Services.AddScoped<ITrelisTimedActionsService, TrelisTimedActionsService>();
            builder.Services.AddScoped<ITaskManager, TaskManager>();
            builder.Services.AddScoped<IEscalationService, EscalationService>();
            builder.Services.AddScoped<CprEscalationManager>();
            builder.Services.AddScoped<GbcEscalationManager>();
            builder.Services.AddScoped<CoiOtherEscalationService>();
            builder.Services.AddScoped<TravelEscalationService>();
            builder.Services.AddScoped<AllowanceClaimsEscalationService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IEscalationFactoryService, EscalationFactoryService>();
            builder.Services.AddScoped<IPermissionManager, PermissionManager>();
            builder.Services.AddScoped<IRequestingUserProvider, RequestingUserProvider>();
            builder.Services.AddScoped<IAttachmentService, AttachmentService>();
            builder.Services.AddScoped<IAttachmentRecordService, AttachmentRecordService>();
            builder.Services.AddScoped<IBlobService, BlobService>();
            builder.Services.AddScoped<IMigrationService, MigrationService>();
            builder.Services.AddScoped<IPositionService, PositionService>();
            builder.Services.AddScoped<IWorkflowBtnService, WorkflowBtnService>();
            builder.Services.AddLogging();
            builder.Services.AddScoped<LeaveFormsEscalationManager>();
            builder.Services.AddScoped<RosterChangeEscalationManager>();           
            builder.Services.AddScoped<AllowanceClaimsFormsEscalationServiceManager>();
            builder.Services.AddScoped<IRecruitmentEformService, RecruitmentEformService>();
            builder.Services.AddScoped<HomeGaragingApprovalService>();
            builder.Services.AddScoped<HomeGaragingFormService>();
            builder.Services.AddScoped<NonStandardHardwareAcquisitionRequestApprovalService>();
            builder.Services.AddScoped<NonStandardHardwareAcquisitionRequestFormService>();
            builder.Services.AddScoped<NcLocationService>();
            builder.Services.AddScoped<HomeGaragingMessageBuilder>();
            builder.Services.AddScoped<HomeGaragingFormsEscalationManager>();
            builder.Services.AddScoped<WHSIRMessageBuilder>();
            builder.Services.AddScoped<WHSIREscalationService>();
            builder.Services.AddScoped<NonStandardHardwareAcquisitionRequestMessageBuilder>();

        }
    }
}