using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eforms_middleware.Services
{
    public class FormInfoService : IFormInfoService
    {
        private readonly AppDbContext _dbContext;
        private readonly IRepository<FormInfo> _formInfoRepository;
        private readonly IRequestingUserProvider _requestingUserProvider;
        private readonly IEmployeeService _employeeService;
        public FormInfoService(AppDbContext dbContext,
            IRepository<FormInfo> formInfoRepository,
            IRequestingUserProvider requestingUserProvider,
            IEmployeeService employeeService)
        {
            _dbContext = dbContext;
            _formInfoRepository = formInfoRepository;
            _requestingUserProvider = requestingUserProvider;
            _employeeService = employeeService;
        }

        public async Task<IEnumerable<UsersFormStoredProcedureDto>> GetUsersFormsAsync(string userId)
        {
            var requestingUser = await _requestingUserProvider.GetRequestingUser();
            var specification = new FormInfoSpecification(null, includePermissions: true, requestingUser: requestingUser);
            var forms = await _formInfoRepository.ListAsync(specification);
            var tableInfo = new List<UsersFormStoredProcedureDto>();
            foreach (var form in forms)
            {
                var childFormType = "";
                //44 - 45 is Home Garaging and Non-Standard Hardware Acquisition Request
                if (form.AllForms.ParentAllFormsId == 44 || form.AllForms.ParentAllFormsId == 46) {
                    childFormType = form.AllForms.FormTitle; 
                }
                else {
                    childFormType = form.AllForms.ParentAllFormsId.HasValue ? $"{form.AllForms.ParentAllForms.FormTitle} - {form.AllForms.FormTitle}" : form.AllForms.FormTitle;
                }
                var dto = new UsersFormStoredProcedureDto
                {
                    AllFormsID = form.AllFormsId,
                    ChildFormType = childFormType,
                    CanAction = form.FormPermissions.Any(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable && x.UserHasPermission(requestingUser)),
                    CanRecall = false,
                    FormInfoID = form.FormInfoId,
                    // TODO see http://thunderbolt:8080/browse/CIE-584 amend the below to be single. People are adding multiple owners so will need to prevent that first
                    FormOwnerName = form.FormPermissions.FirstOrDefault(x => x.IsOwner)?.Name,
                    FormStatusID = form.FormStatusId,
                    // TODO this needs to be connected to the form statuses in the DB and then match up with the front end for what is presented to the user
                    Status = ((FormStatus)form.FormStatusId).ToString(),
                    SubmittedDate = form.SubmittedDate,
                    MyForm = form.FormPermissions.Any(x => x.IsOwner && x.UserHasPermission(requestingUser)),
                    MyGroup = form.FormPermissions.Any(x => x.GroupId != null && x.UserHasPermission(requestingUser)),
                    NextApprover = form.FormPermissions.FirstOrDefault(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable)?.Name
                };
                tableInfo.Add(dto);
            }
            return tableInfo;
        }

        public IEnumerable<RefDirectorate> GetAllDirectorate()
        {
            var result = _dbContext.Set<RefDirectorate>().ToList();
            return result;
        }

        public IEnumerable<AllForm> GetAllFormTypes()
        {
            var result = _dbContext.Set<AllForm>().ToList();
            return result;
        }

        public IEnumerable<RefFormStatus> GetAllFormStatuses()
        {
            var result = _dbContext.Set<RefFormStatus>().ToList();
            return result;
        }

        public async Task<FormInfo> SaveFormInfoAsync(FormInfoUpdate request, FormInfo dbRecord)
        {
            if (request.FormDetails.FormInfoId == null)
            {
                dbRecord = await CreateFormInfoAsync(dbRecord);
            }
            else
            {
                var requestingUser = await _requestingUserProvider.GetRequestingUser();
                dbRecord.Modified = DateTime.Now;
                dbRecord.ModifiedBy = requestingUser?.EmployeeEmail ?? "System";
                _formInfoRepository.Update(dbRecord);
            }

            return dbRecord;
        }

        public async Task<FormInfo> SaveLeaveFormsInfoAsync(FormInfoUpdate request, FormInfo dbRecord)
        {
            if (request.FormDetails.FormInfoId == null)
            {
                dbRecord = await CreateFormInfoAsync(dbRecord);
            }
            else
            {
                var requestingUser = await _requestingUserProvider.GetRequestingUser();
                dbRecord.Modified = DateTime.Now;
                dbRecord.ModifiedBy = requestingUser?.EmployeeEmail ?? "System";
                //Save purchased and leave cash out data from summary page when status is completed
                if ((dbRecord.AllFormsId == Convert.ToInt32(FormType.LcR_DSA) || dbRecord.AllFormsId == Convert.ToInt32(FormType.LcR_PLA) || dbRecord.AllFormsId == Convert.ToInt32(FormType.LcR_LCO) || dbRecord.AllFormsId == Convert.ToInt32(FormType.LcR_PLS)) && (dbRecord.FormStatusId == 4 || dbRecord.FormStatusId == 3))
                {
                    dbRecord.Response = request.FormDetails.Response;
                }
                _formInfoRepository.Update(dbRecord);
            }
            return dbRecord;
        }

        private async Task<FormInfo> CreateFormInfoAsync(FormInfo dbRecord)
        {
            var requestingUser = await _requestingUserProvider.GetRequestingUser();
            var now = DateTime.Now;

            dbRecord.ActiveRecord = true;
            dbRecord.Created = now;
            dbRecord.CreatedBy = requestingUser.EmployeeEmail;
            dbRecord.Modified = now;
            dbRecord.ModifiedBy = requestingUser.EmployeeEmail;
            dbRecord.FormItemId = 1;
            dbRecord.FormOwnerDirectorate = requestingUser.Directorate;
            dbRecord.FormOwnerName = requestingUser.EmployeePreferredFullName;
            dbRecord.FormOwnerEmail = requestingUser.EmployeeEmail;
            dbRecord.FormOwnerEmployeeNo = requestingUser.EmployeeNumber;
            dbRecord.FormOwnerPositionTitle = requestingUser.EmployeePositionTitle;
            dbRecord.FormOwnerPositionNo = requestingUser.EmployeePositionNumber;

            return _formInfoRepository.Create(dbRecord);
        }

        public async Task<FormInfo> GetExistingOrNewFormInfoAsync(FormInfoUpdate request, bool addAttachments = false)
        {
            var formDetails = request.FormDetails;
            if (!request.FormDetails.FormInfoId.HasValue) return new FormInfo { AllFormsId = formDetails.AllFormsId, FormStatusId = (int)FormStatus.Unsubmitted };
            var existingWithPermissionsSpec = new FormInfoSpecification(request.FormDetails.FormInfoId.Value, includePermissions: true, includeAttachments: addAttachments);
            var existingForm = await _formInfoRepository.FirstOrDefaultAsync(existingWithPermissionsSpec);
            return existingForm;
        }

        public async Task<FormInfo> GetFormInfoByIdAsync(int formInfoId)
        {
            return await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == formInfoId);
        }

        public async Task<FormDetails> GetFormByIdAsync(int formInfoId)
        {
            var requestingUser = await _requestingUserProvider.GetRequestingUser();
            var specification = new FormInfoSpecification(formInfoId, includePermissions: true, includeButtons: true);
            var formInfo = await _formInfoRepository.FirstOrDefaultAsync(specification);
            var workflowButtons = new List<WorkflowButton>();
            FormPermission formPermission = null;
            var isOdgGroupUser = await _employeeService.IsOdgGroupUser(requestingUser.ActiveDirectoryId);

            foreach (var permission in formInfo.FormPermissions)
            {
                if (!permission.UserHasPermission(requestingUser)) continue;
                var isActionablePermission = permission.PermissionFlag == (byte)PermissionFlag.UserActionable;
                if (isActionablePermission || formPermission is null) formPermission = permission;

                if (permission.IsOwner && formInfo.FormStatusId is not 2 and not 4)
                {
                    workflowButtons.Add(WorkflowButton.RecallButton());
                    workflowButtons.Add(WorkflowButton.CancelButton());
                }

                if (isActionablePermission || permission.PermissionFlag == (byte)PermissionFlag.GroupFallback)
                {
                    workflowButtons.AddRange(permission.WorkflowBtns.Where(x => x.IsActive.Value).Select(x => new WorkflowButton(x)));
                }
            }
            if (formPermission == null)
            {
                return null;
            }
            string directorate = null;
            if (int.TryParse(formInfo.FormOwnerDirectorate, out var directorateId))
            {
                directorate =
                    _dbContext.RefDirectorates.SingleOrDefault(d => d.RefDirectoratesId == directorateId)?.Directorate;
            }
            var status = _dbContext.RefFormStatuses.Single(s => s.RefStatusesId == formInfo.FormStatusId);

            var dt = formInfo.FormPermissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
            var actioningPermission = dt.FirstOrDefault();
            var formOwner = formInfo.FormPermissions.SingleOrDefault(x => x.IsOwner);
            var result = new FormDetails
            {
                FormItemId = formInfo.FormItemId,
                FormInfoID = formInfo.FormInfoId,
                Created = formInfo.Created,
                Modified = formInfo.Modified,
                ActiveRecord = formInfo.ActiveRecord,
                Response = formInfo.Response,
                CompletedDate = formInfo.CompletedDate,
                CreatedBy = formInfo.CreatedBy,
                AllFormsID = formInfo.AllFormsId,
                FormApprovers = formInfo.FormApprovers,
                FormReaders = formInfo.FormReaders,
                ModifiedBy = formInfo.ModifiedBy,
                FormSubStatus = formInfo.FormSubStatus,
                NextApprover = actioningPermission?.PermissionActionerDescription,
                SubmittedDate = formInfo.SubmittedDate,
                FormOwnerDirectorate = formInfo.FormOwnerDirectorate,
                InitiatedForDirectorate = directorate,
                FormOwnerEmail = formInfo.FormOwnerEmail,
                FormOwnerName = formOwner?.PermissionActionerDescription,
                InitiatedForEmail = formInfo.InitiatedForEmail,
                FormStatusID = status.Status,
                InitiatedForName = formInfo.InitiatedForName,
                NextApprovalLevel = formInfo.NextApprovalLevel,
                FormOwnerEmployeeNo = formInfo.FormOwnerEmployeeNo,
                FormOwnerPositionNo = formInfo.FormOwnerPositionNo,
                FormOwnerPositionTitle = formInfo.FormOwnerPositionTitle,
                PDFGuid = formInfo.Pdfguid,
                CanAction = actioningPermission?.UserHasPermission(requestingUser) ?? false,
                CanRecall = (formOwner?.UserHasPermission(requestingUser) ?? false) && formInfo.FormStatusId is not 2 and not 4,
                ChildFormType = formInfo.ChildFormType,
                WorkflowButtons = workflowButtons,
                IsOdgReview = isOdgGroupUser
            };
            return result;
        }
    }
}