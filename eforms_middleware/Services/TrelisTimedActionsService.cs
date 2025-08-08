using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Constants.E29;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.MasterData;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace eforms_middleware.Services
{
    public class TrelisTimedActionsService : ITrelisTimedActionsService
    {
        private readonly IMessageFactoryService _messageFactoryService;
        private readonly IRepository<FormInfo> _formInfoRepository;
        private readonly ISpViewRepository<TrelisReportModelDto> _trelisViewRepo;
        private readonly IRepository<FormPermission> _formPermissionRepo;
        private readonly ILogger<TrelisTimedActionsService> _logger;

        public TrelisTimedActionsService(IMessageFactoryService messageFactoryService
            , IRepository<FormInfo> formInfoRepository, ILogger<TrelisTimedActionsService> logger,
            ISpViewRepository<TrelisReportModelDto> trelisViewRepo, IRepository<FormPermission> formPermissionRepo)
        {
            _messageFactoryService = messageFactoryService;
            _formInfoRepository = formInfoRepository;
            _logger = logger;
            _trelisViewRepo = trelisViewRepo;
            _formPermissionRepo = formPermissionRepo;
        }
        
        public async Task SendReminderEmailsAsync()
        {
            var fiveDaysAgo = DateTime.Today.AddDays(-5);
            var twoDaysAgo = DateTime.Today.AddDays(-2);
            var overdueSpecification = new E29OverDueSpecification(fiveDaysAgo, twoDaysAgo);
            var overdueForms = await _formInfoRepository.ListAsync(overdueSpecification);
            _logger.LogInformation("Preparing {FormCount} forms", overdueForms.Count);
            foreach (var form in overdueForms)
            {
                try
                {
                    if (form.Created.Value!.Date == fiveDaysAgo)
                    {
                        // Send Management email
                        await _messageFactoryService.SendEmailAsync(form,
                            new FormInfoUpdate
                            {
                                FormAction = nameof(FormStatus.Unsubmitted),
                                FormDetails = new FormDetailsRequest
                                    { NextApprover = E29Constants.TrelisAccessManagementGroupMail }
                            });
                    }
                    else if (form.Created.Value.Date == twoDaysAgo)
                    {
                        // Send the other email
                        var action = string.IsNullOrWhiteSpace(form.NextApprover)
                            ? nameof(FormStatus.Unsubmitted)
                            : nameof(FormStatus.Delegate);
                        await _messageFactoryService.SendEmailAsync(form,
                            new FormInfoUpdate
                            {
                                FormAction = action,
                                FormDetails = new FormDetailsRequest()
                            });
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error trying to send reminder for Trelis Access Report: {FormId}", form.FormInfoId);
                }
            }
        }
        
        public async Task CreateTrelisForms(int? branchId = null)
        {
            var now = DateTime.Now;
            var nextPeriod = DateTime.Today.AddMonths(1);
            var minusOneMonth = DateTime.Today.AddMonths(-1);
            var startOfLastMonth = new DateTime(minusOneMonth.Year, minusOneMonth.Month, 1);
            
            try
            {
                var specification = new FormsTypeFromPeriodSpecification((int)FormType.e29, startOfLastMonth);
                var previousMonthsForms = await _formInfoRepository.ListAsync(specification);

                var previousForms = previousMonthsForms.Select(x =>
                {
                    var form = JsonConvert.DeserializeObject<E29Form>(x.Response);
                    return new
                    {
                        OldId = x.FormInfoId,
                        form.BranchName,
                        form.BranchId,
                        Form = form
                    };
                });
                var trelisBranches = await _trelisViewRepo.ToListAsync("SELECT * FROM dbo.vw_get_trelis_generation_info");

                var trellisForms = trelisBranches.Where(x => branchId == null || branchId == x.TrelisBranchId).Select(b =>
                {
                    var previousMonthsForm = previousForms.Where(x => x.BranchId == b.TrelisBranchId).MaxBy(x=> x.OldId);
                    var preservedUsers = previousMonthsForm?.Form.Users
                                             .Where(x => x.RemovedReason != 1 && x.RemovedReason != 2 && x.RemovedReason != 4).ToList() ??
                                         new List<TeamMember>();
                    var response = new E29Form
                    {
                        BranchName = b.Branch,
                        BranchId = b.TrelisBranchId,
                        Year = nextPeriod.Year,
                        Month = nextPeriod.Month,
                        Users = preservedUsers,
                        PreviousMonthFormId = previousMonthsForm?.OldId
                    };

                    return new
                    {
                        FormInfo = new FormInfo
                        {
                            FormStatusId = (int)FormStatus.Unsubmitted,
                            FormSubStatus = nameof(FormStatus.Unsubmitted),
                            Response = JsonConvert.SerializeObject(response),
                            ActiveRecord = true,
                            CreatedBy = "E29 Create Function",
                            FormOwnerPositionTitle = b.PositionTitle,
                            AllFormsId = (int)FormType.e29,
                            FormOwnerEmail = b.EmployeeEmail,
                            FormOwnerName = b.EmployeeName,
                            FormOwnerEmployeeNo = b.EmployeeNumber,
                            FormOwnerDirectorate = b.Directorate,
                            FormItemId = 1,
                            InitiatedForDirectorate = b.Directorate,
                            Created = now,
                            NextApprover = b.EmployeeEmail == null ? E29Constants.TrelisAccessManagementGroupMail : null
                        },
                        PositionId = b.PositionId
                    };
                });

                foreach (var trelisForm in trellisForms)
                {
                    var newForm = await _formInfoRepository.AddAsync(trelisForm.FormInfo);
                    var formPermissions = new[]
                    {
                        new FormPermission
                            { FormId = newForm.FormInfoId, PermissionFlag = (byte)PermissionFlag.View, GroupId = E29Constants.TRELIS_ACCESS_MANAGEMENT_ID },
                        new FormPermission
                            { FormId = newForm.FormInfoId, PermissionFlag = (byte)PermissionFlag.UserActionable, PositionId = trelisForm.PositionId, IsOwner = true}
                    };
                    await _formPermissionRepo.AddRangeAsync(formPermissions);
                    await _messageFactoryService.SendEmailAsync(newForm,
                        new FormInfoUpdate
                        {
                            FormAction = nameof(FormStatus.Unsubmitted),
                            FormDetails = new FormDetailsRequest()
                        });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error in {nameof(TrelisMonthlyReturnFunctions)}");
                throw;
            }
        }
    }
}