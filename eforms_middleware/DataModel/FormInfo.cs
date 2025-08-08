using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using System;
using System.Collections.Generic;

namespace eforms_middleware.DataModel
{
    public class FormInfoRequest
    {
        public int? FormId { get; set; }
        public int AllFormsId { get; set; }
        public int StatusId { get; set; }
        public string FormStatus { get; set; }
        public string FormOwnerEmail { get; set; }
        public string FormSubStatus { get; set; }
        public string ActionBy { get; set; }
        public string Response { get; set; }
        public string AdditionalInfo { get; set; }
        public string ReasonForRejection { get; set; }
        public string NextApproverEmail { get; set; }
        public string NextApproverTitle { get; set; }
        public bool IsUpdateAllowed { get; set; }
        public string BaseUrl { get; set; }
        public bool AllowPersonalInfo { get; set; }
        public bool AllowEscalation { get; set; }
        public bool AllowReminder { get; set; }
        public string PreviousApprover { get; set; }
    }

    public class FormInfoUpdate
    {
        public FormDetailsRequest FormDetails { get; set; }
        public string FormAction { get; set; }
        public string UserId { get; set; }
    }

    public class TravelFormResponseModel
    {
        public string IsMainTraveller { get; set; }
        public List<UserIdentifier> AllTravellers { get; set; }
        public string EmailAddress { get; set; }
        public string PersonalMobileNumber { get; set; }
        public string ReasonForTravel { get; set; }
        public string ModeOfTravel { get; set; }
        public string VehicleType { get; set; }
        public string DestinationType { get; set; }
        public List<Attachments> Attachments { get; set; }
        public string IsConferenceDelegateOrPresenter { get; set; }
        public string IsNoCostFlightsProvided { get; set; }
        public string NoCostMealsProvided { get; set; }
        public string IsDoTOutcomeRequired { get; set; }
        public string IsRolePerformanceEssential { get; set; }
        public string ExtendForPrivateUse { get; set; }
        public string IsLeaveExceed { get; set; }
        public string FairAcknowledge { get; set; }
        public string NoExtraCostCertify { get; set; }
        public string NoInsurnceAcknowledge { get; set; }
        public string ExtraTravelDesc { get; set; }
        public bool IsAdditionalTravellerAllowed { get; set; }
        public DateTime? TravelStartDate { get; set; }
        public DateTime? TravelEndDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public ItineraryDetails[] ItineraryDetails { get; set; }
        public string Flight { get; set; }
        public string RegistrationFees { get; set; }
        public string Parking { get; set; }
        public string Taxi { get; set; }
        public string CarHire { get; set; }
        public string Fuel { get; set; }
        public string TotalAccommodation { get; set; }
        public string TotalMeal { get; set; }
        public string TotalIncidentals { get; set; }
        public string TotalActualAccommodation { get; set; }
        public string TotalActualMeal { get; set; }
        public string TotalActualIncidentals { get; set; }
        public string TotalEstimatedTravelCosts { get; set; }
        public string TotalEstimatedAllowanceCost { get; set; }
        public string TotalActualAllowanceCost { get; set; }
        public string TotalCampingPayable { get; set; }
        public string TotalMealsIncidentalCosts { get; set; }
        public CampingRate[] CampingRate { get; set; }
        public string Declaration { get; set; }
        public int? SalaryRateId { get; set; }
        public bool ShowActualCost { get; set; }
        public bool HasReconsiled { get; set; } = false;
        public bool ReadyForReconsiliation { get; set; } = false;
        public string TotalTax { get; set; }
        public string TotalClaim { get; set; }
        public PayCodes[] PayCodes { get; set; }

        public string DelegatedFromEmail { get; set; }

        public CoaData CoaData { get; set; }
    }

    public class CoaData
    {
        public string isCostCentreOverride { get; set; }
        public string fund { get; set; }
        public string activity { get; set; }
        public string costCentre { get; set; }
        public string location { get; set; }
        public string project { get; set; }
        public string account { get; set; }
    }

    public class PayCodes
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string Cost { get; set; }
    }

    public class Attachments
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class ItineraryDetails
    {
        public string Date { get; set; }
        public string Location { get; set; }
        public string[] MealAllowance { get; set; }
        public string DailyComment { get; set; }
        public decimal? Meal { get; set; }
        public decimal? Incidentals { get; set; }
        public decimal? Accomodation { get; set; }
        public decimal? MealAto { get; set; }
        public decimal? IncidentalsAto { get; set; }
        public decimal? AccomodationAto { get; set; }
        public bool? IsCamping { get; set; }
        public string MealsProvided { get; set; }
        public string TypeOfCamping { get; set; }
        public string CampParallel { get; set; }
        public string CampLocation { get; set; }
        public string TotalClaim { get; set; }
        public string TotalTax { get; set; }
        public string MealTax { get; set; }
        public string IncidentalsTax { get; set; }
        public string AccomodationTax { get; set; }
    }

    public class CampingRate
    {
        public string Date { get; set; }
        public decimal? TotalPayable { get; set; }
        public decimal? CampRate { get; set; }
        public decimal? TimeRate { get; set; }
        public decimal? CookPercent { get; set; }
        public bool? IsCamping { get; set; }
        public string CampId { get; set; }
        public decimal? Units { get; set; }

    }
    public class StatusBtnData
    {
        public List<StatusBtnModel> StatusBtnModel { get; set; }
        public bool IsDelegate { get; set; }
        public bool IsIndependentReview { get; set; }
        public int? PositionId { get; set; }
        public Guid? GroupId { get; set; }
    }

    public class StatusBtnModel
    {
        public int StatusId { get; set; }
        public string FormSubStatus { get; set; }
        public string BtnText { get; set; }
        public bool IsReject { get; set; }
        public bool IsUserDialog { get; set; }
        public bool HasDeclaration { get; set; }
    }

    public class WorkflowModel
    {
        public FormInfoRequest FormInfoRequest { get; set; }
        public StatusBtnData StatusBtnData { get; set; }
        public EmailNotificationModel EmailNotificationModel { get; set; }
    }

    public class EmailNotificationModel
    {
        public string FormOwnerName { get; set; }
        public string ReceiverName { get; set; }
        public string SummaryUrl { get; set; }
        public string EditFormUrl { get; set; }
        public int FormInfoId { get; set; }
        public FormStatus FormStatus { get; set; }
        public List<EmailSendType> EmailSendType { get; set; }
    }

    public class ApprovalResult
    {
        public string RequestBody { get; set; }
        public string CurrentApprover { get; set; }
    }

    public class SetApprovalResult
    {
        public FormInfoInsertModel FormInfoInsertModel { get; set; }
        public bool IsUpdated { get; set; }
    }

    public class FormInfoInsertModel
    {
        public FormDetails FormDetails { get; set; }
        public string FormAction { get; set; }
        public string AdditionalInfo { get; set; }
        public string AdditionalComments { get; set; }


        public FormDetailsRequest ToFormDetailsRequest()
        {
            return new FormDetailsRequest
            {
                AllFormsId = FormDetails.AllFormsID,
                FormInfoId = FormDetails.FormInfoID,
                NextApprover = FormDetails.NextApprover,
                Response = FormDetails.Response
            };
        }
    }

    public class FormDetailsRequest
    {
        public int AllFormsId { get; set; }
        public int? FormInfoId { get; set; }
        public string NextApprover { get; set; }
        public string Response { get; set; }
    }

    public class FormDetails
    {
        public int? FormInfoID { get; set; }
        public int AllFormsID { get; set; }
        public string ChildFormType { get; set; }
        public int? FormItemId { get; set; }
        public string FormOwnerName { get; set; }
        public string FormOwnerEmail { get; set; }
        public string FormOwnerDirectorate { get; set; }
        public string FormOwnerPositionNo { get; set; }
        public string FormOwnerEmployeeNo { get; set; }
        public string FormOwnerPositionTitle { get; set; }
        public string FormStatusID { get; set; }
        public string FormSubStatus { get; set; }
        public string InitiatedForName { get; set; }
        public string InitiatedForEmail { get; set; }
        public string InitiatedForDirectorate { get; set; }
        public string FormApprovers { get; set; }
        public string FormReaders { get; set; }
        public DateTime? Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? Modified { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string PDFGuid { get; set; }
        public bool? ActiveRecord { get; set; }
        public string NextApprovalLevel { get; set; }
        public string NextApprover { get; set; }
        public string Response { get; set; }
        public bool CanAction { get; set; }
        public bool CanRecall { get; set; }
        public bool IsOdgReview { get; set; }
        public IList<WorkflowButton> WorkflowButtons { get; set; } = new List<WorkflowButton>();
    }

    public class WorkflowButton
    {
        public long Id { get; set; }
        public int PermissionId { get; set; }
        public int StatusId { get; set; }
        public bool IsActive { get; set; }
        public string ButtonText { get; set; }
        public string FormSubStatus { get; set; }
        public bool HasDeclaration { get; set; }

        private WorkflowButton()
        {

        }

        public WorkflowButton(WorkflowBtn entity)
        {
            Id = entity.Id;
            PermissionId = entity.PermisionId;
            StatusId = entity.StatusId;
            IsActive = entity.IsActive ?? false;
            ButtonText = entity.BtnText;
            FormSubStatus = entity.FormSubStatus;
            HasDeclaration = entity.HasDeclaration ?? false;
        }

        public static WorkflowButton RecallButton()
        {
            var recall = new WorkflowButton();
            recall.ButtonText = "Recall";
            recall.IsActive = true;
            recall.StatusId = (int)FormStatus.Recall;
            recall.FormSubStatus = FormStatus.Recall.ToString();
            return recall;
        }
        public static WorkflowButton CancelButton()
        {
            var recall = new WorkflowButton();
            recall.ButtonText = "Cancel";
            recall.IsActive = true;
            recall.StatusId = (int)FormStatus.Cancel;
            recall.FormSubStatus = FormStatus.Cancel.ToString();
            return recall;
        }
    }

    public class FormInfoGetModel
    {
        public int? FormInfoId { get; set; }
        public int AllFormsID { get; set; }
        public string FormItemId { get; set; }
        public string FormOwnerName { get; set; }
        public string FormOwnerEmail { get; set; }
        public string FormOwnerDirectorate { get; set; }
        public string FormOwnerPositionNo { get; set; }
        public string FormOwnerEmployeeNo { get; set; }
        public string FormOwnerPositionTitle { get; set; }
        public string FormStatusID { get; set; }
        public string FormSubStatus { get; set; }
        public string InitiatedForName { get; set; }
        public string InitiatedForEmail { get; set; }
        public string InitiatedForDirectorate { get; set; }
        public string FormApprovers { get; set; }
        public string FormReaders { get; set; }
        public DateTime? Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? Modified { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string PDFGuid { get; set; }
        public bool ActiveRecord { get; set; }
        public string NextApprovalLevel { get; set; }
        public string NextApprover { get; set; }
        public string Response { get; set; }
    }

    public class AllFormInfoModel
    {
        public int FormInfoID { get; set; }
        public int AllFormsID { get; set; }
        public string FormItemId { get; set; }
        public string FormOwnerName { get; set; }
        public string FormStatusID { get; set; }
        public string FormSubStatus { get; set; }
        public string InitiatedForDirectorateID { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string NextApprover { get; set; }

        public string FormOwnerEmail { get; set; }
    }

    public class CreateUpdateOutcomeModel
    {
        public int AllFormsID { get; set; }
        public int FormInfoID { get; set; }
        public int FormItemId { get; set; }
        public string FormAction { get; set; }
    }

    public class GBHModel : COIMain
    {
        public string DateOfOffer { get; set; }
        public string TypeOfOffer { get; set; }
        public string SourceOfOffer { get; set; }
        public string SupplierContractorStatus { get; set; }
        public string SourcePerson { get; set; }
        public string SourceOrganisation { get; set; }
        public string SourceContactInformation { get; set; }
        public string DescriptionOfOffer { get; set; }
        public string EstimatedValue { get; set; }
        public UserIdentifier[] AdditionalOfficers { get; set; }
        public string RelatedOffers { get; set; }
        public string AcceptOffer { get; set; }
        public string AcceptanceReason { get; set; }
        public string PhysicalLocation { get; set; }
        public string ReasonForLocation { get; set; }
        public string FutureAcceptanceDate { get; set; }
        public string TypeOfConflict { get; set; }
        public string AssessmentReason { get; set; }
        public string ManageConflictDescription { get; set; }
        public string IsAccepted { get; set; }
        public bool IsTier3Actioned { get; set; }
        public bool IsRequestOnBehalf { get; set; }
        public IList<UserIdentifier> requestOnBehalf { get; set; } = new List<UserIdentifier>();
    }

    public class RecruitmentModel : COIMain
    {
        public string BcrReference { get; set; }
        public string PositionNumber { get; set; }
        public string PositionTitle { get; set; }
        public string Directorate { get; set; }
        public UserIdentifier[] PanelMembers { get; set; }
        public ExternalPanelMember[] ExternalPanelMember { get; set; } = Array.Empty<ExternalPanelMember>();
        public UserIdentifier PanelChair { get; set; }
        public string HasConflictOfInterest { get; set; }
        public string TypeOfConflict { get; set; }
        public string RelationshipToApplicant { get; set; }
        public string ConflictDescription { get; set; }
        public string ProposedPlanToManageConflict { get; set; }
        public string IsAccepted { get; set; }
        public string SelectedExternalMember { get; set; }
        public string HasExternalConflict { get; set; }
        public bool IsRequestOnBehalf { get; set; }
        public IList<UserIdentifier> requestOnBehalf { get; set; } = new List<UserIdentifier>();

    }

    public class UserIdentifier
    {
        public Guid ActiveDirectoryId { get; set; }
        public string EmployeeEmail { get; set; }
        public string EmployeeFullName { get; set; }
    }


    public class SecondaryEmploymentModel : COIMain
    {
        public string EmploymentDateFrom { get; set; }
        public string EmploymentDateTo { get; set; }
        public string TypeOfSecondaryEmployment { get; set; }
        public string NameOfEmployerOrg { get; set; }
        public string DescriptionOfEngagement { get; set; }
        public string TypeOfConflict { get; set; }
        public string ReasonsForAssessment { get; set; }
        public string PlanToManageConflict { get; set; }
        public string IsAccepted { get; set; }
        public bool IsRequestOnBehalf { get; set; }
        public IList<UserIdentifier> requestOnBehalf { get; set; } = new List<UserIdentifier>();

    }

    public class ExternalPanelMember
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class COIMain
    {
        public string ConflictOfInterestRequestType { get; set; }
        public string ReasonForDecision { get; set; }
        public IList<AttachmentResult> Attachments { get; set; } = new List<AttachmentResult>();
    }

    public class BlobData
    {
        public string Name { get; set; }

        public Uri Url { get; set; }
    }

    public class LCOMain
    {
        public string leaveApplicationRequestType { get; set; }
        public string ReasonForDecision { get; set; }
        public IList<AttachmentResult> Attachments { get; set; } = new List<AttachmentResult>();
    }

    public class LeaveAmendmentModel : LCOMain
    {

        public string LeaveApplicationRequestType { get; set; }
        public string isLeaveRequesterIsManager { get; set; }
        public string leaveApplicationType { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        //public string allEmployees { get; set; }
        //public UserIdentifier[] PanelMembers { get; set; }
        public LeaveBookingValues[] LeaveBookingValues { get; set; } = Array.Empty<LeaveBookingValues>();
        public LeaveBookingValues[] ReplacementLeaveBookingValues { get; set; } = Array.Empty<LeaveBookingValues>();
        public List<UserIdentifier> allEmployees { get; set; }
        public string ReasonForAmendmentCancellation { get; set; }
        public string ReasonForLeaveUtilizing { get; set; }
        public string isRdos { get; set; }
        public RdosValues[] RdosValues { get; set; } = Array.Empty<RdosValues>();


    }
    public class ProRataModel : LCOMain
    {
        public string LeaveApplicationRequestType { get; set; }
        public string IsLeaveRequesterIsManager { get; set; }
        public string LeaveApplicationType { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        //public UserIdentifier allEmployees { get; set; }

        public List<UserIdentifier> allEmployees { get; set; }

        public CalculatorEligibilityValues[] CalculatorEligibilityValues { get; set; } = Array.Empty<CalculatorEligibilityValues>();
        public ProRataFormsValues[] proRataChildFormValues { get; set; } = Array.Empty<ProRataFormsValues>();
    }

    public class PurchasedLeaveModel : LCOMain
    {

        public string LeaveApplicationRequestType { get; set; }
        public string IsLeaveRequesterIsManager { get; set; }
        public string LeaveApplicationType { get; set; }

        public PurchasedLeaveBookingValues[] PurchasedLeaveBookingValues { get; set; } = Array.Empty<PurchasedLeaveBookingValues>();
        public AnnualLeaveBookingValues[] AnnualLeaveBookingValues { get; set; } = Array.Empty<AnnualLeaveBookingValues>();
        public List<UserIdentifier> allEmployees { get; set; }
        public string QuaterValue { get; set; }
        public string NominateStartDate { get; set; }
        public string AdditionalInfo { get; set; }
        public string SelectWeekOne { get; set; }
        public string SelectWeekTwo { get; set; }
        public string SelectWeekThree { get; set; }
        public string SelectWeekFour { get; set; }
        public string SelectedWeekValue { get; set; }

        public string CommencementDateValue { get; set; }
        public string CommencementOfDeductionsValue { get; set; }
        public string CessationOfDeductionsValue { get; set; }

        public string PurchasedLeaveFirstDayBookingValidation { get; set; }
        public string PurchasedLeaveLastDayBookingValidation { get; set; }
        public string AnnualLeaveFirstDayBookingValidation { get; set; }
        public string AnnualLeaveLastDayBookingValidation { get; set; }

        public string CessationDateOfDeductionsValue { get; set; }
        public string NumberOfHoursPurchasedValue { get; set; }
        public string DeductionAmountValue { get; set; }

    }

    public class LeaveCashOutModel : LCOMain
    {

        public string LeaveApplicationRequestType { get; set; }
        public string IsLeaveRequesterIsManager { get; set; }
        public string LeaveApplicationType { get; set; }
        public string HoursToCashOut { get; set; }
        public List<UserIdentifier> allEmployees { get; set; }

        public string GrossPaymentValue { get; set; }
        public string TaxWithheldValue { get; set; }
        public string NetPaymentValue { get; set; }
        public string PaymentDateValue { get; set; }
        public string ExcisedbyCalendarDayvalue { get; set; }
    }
    public class PurchasedLeaveBookingValues
    {
        public string purchasedLeaveFirstDayLeave { get; set; }
        public string purchasedLeaveLastDayLeave { get; set; }
    }
    public class AnnualLeaveBookingValues
    {
        public string AnnualLeaveFirstDayLeave { get; set; }
        public string AnnualLeaveLastDayLeave { get; set; }
    }

    public class LeaveWithoutPayModel : LCOMain
    {
        public string LeaveApplicationRequestType { get; set; }
        public string IsLeaveRequesterIsManager { get; set; }
        public string leaveApplicationType { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        public string IsDistrictAllowance { get; set; }
        public string IsLeavingDistrict { get; set; }
        public string DateLeavingDistrict { get; set; }
        public string DateReturningDistrict { get; set; }
        public string DateFirstDayLeave { get; set; }
        public string DateLastDayLeave { get; set; }
        public string ReasonForLeave { get; set; }
        public int DifferenceInDays { get; set; }
        public List<UserIdentifier> allEmployees { get; set; }
    }
    public class MaternityLeaveModel : LCOMain
    {
        public string LeaveApplicationRequestType { get; set; }
        public string isLeaveRequesterIsManager { get; set; }
        public string leaveApplicationType { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }

        public string IsDistrictAllowance { get; set; }
        public string IsLeavingDistrict { get; set; }
        public string DateLeavingDistrict { get; set; }
        public string DateReturningDistrict { get; set; }
        public string LeaveType { get; set; }

        public string DateFirstDayLeave { get; set; }
        public string DateLastDayLeave { get; set; }

        public string PaymentOption { get; set; }

        public List<UserIdentifier> allEmployees { get; set; }
        public int DifferenceInDays { get; set; }

    }
    public class LeaveBookingValues
    {
        public string LeaveType { get; set; }
        public string LeaveReason { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Hours { get; set; }
    }

    public class ProRataFormsValues
    {
        public string LeaveType { get; set; }
        public string Comments { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Hours { get; set; }
    }
    public class CalculatorEligibilityValues
    {
        public string Age { get; set; }
        public string DateOfBirth { get; set; }
        public string ProRataLSL { get; set; }
    }

    public class RdosValues
    {
        public string rdosDate { get; set; }

    }

    public class LeaveInfoInsertModel
    {
        public FormDetails FormDetails { get; set; }
        public string FormAction { get; set; }
        public string leaveApplicationRequestType { get; set; }
        public string AdditionalInfo { get; set; }
        public string RejectionReason { get; set; }

        public FormDetailsRequest ToFormDetailsRequest()
        {
            return new FormDetailsRequest
            {
                AllFormsId = FormDetails.AllFormsID,
                FormInfoId = FormDetails.FormInfoID,
                NextApprover = FormDetails.NextApprover,
                Response = FormDetails.Response
            };
        }
    }


    public class HGAMain
    {
        public string ReasonForDecision { get; set; }
        public IList<AttachmentResult> Attachments { get; set; } = new List<AttachmentResult>();
    }

    public class HomeGaragingModel : HGAMain
    {
        public string IsLeaveRequesterIsManager { get; set; }
        public string VehicleLocation { get; set; }
        public string ChkLongTermCustodianship { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string IsVehicleParkedOffStreet { get; set; }
        public string IsVehicleParkedUnderCover { get; set; }
        public string Street { get; set; }
        public string Suburb { get; set; }
        public string Postcode { get; set; }
        public string DistanceInKm { get; set; }
        public string PurposeOfHomeGaraging { get; set; }
        public string SupportingEvidence { get; set; }
        public string ListDetailsOfIncidentalPrivateUse { get; set; }
        public string AdditionalComments { get; set; }
        public List<UserIdentifier> AdditionalRecipient { get; set; }

        public VehicleRegistrationValues[] VehicleRegistrationValues { get; set; } = Array.Empty<VehicleRegistrationValues>();
        public string Details { get; set; }
    }


    public class VehicleRegistrationValues
    {
        public string VehicleRegistration { get; set; }
    }

    public class NonStandardHardwareAcquisitionRequestModel : HGAMain
    {
        public string IsRaisingBehalf { get; set; }
        public List<AdfUser> OnBehalfEmployee { get; set; }
        public List<AdfUser> CustodianName { get; set; }
        public StandardSelectModel EquipmentLocation { get; set; }
        public string Level { get; set; }
        public string EquipmentLocationOther { get; set; }
        public string Replacement { get; set; }
        public string DOTiTBarcodeNumber { get; set; }
        public string SerialNumber { get; set; }
        public string EolMonth { get; set; }
        public string Category { get; set; }
        public string ComputerSpecifications { get; set; }
        public string JustificationRequired { get; set; }
        public string Monitor { get; set; }
        public string AdditionalInformation { get; set; }
        public string[] MfdSpecifications { get; set; }
        public string AdditionalTrays { get; set; }
        public string MfdAdditionalInformation { get; set; }
        public string ReceiptPrinterSpecifications { get; set; }
        public CoaData CoaData { get; set; }
        public List<AdfUser> CcManager { get; set; }
        public List<AdfUser> AdditionalNotificationsRecipients { get; set; }
        public string NshasiAdditionalComments { get; set; }
        public string Declaration { get; set; }
    }

    public class HomeGaragingInfoInsertModel
    {
        public FormDetails FormDetails { get; set; }
        public string FormAction { get; set; }
        public string leaveApplicationRequestType { get; set; }
        public string AdditionalInfo { get; set; }
        public string RejectionReason { get; set; }

        public FormDetailsRequest ToFormDetailsRequest()
        {
            return new FormDetailsRequest
            {
                AllFormsId = FormDetails.AllFormsID,
                FormInfoId = FormDetails.FormInfoID,
                NextApprover = FormDetails.NextApprover,
                Response = FormDetails.Response
            };
        }
    }

}


