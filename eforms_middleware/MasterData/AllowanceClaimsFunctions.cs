using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using eforms_middleware.Settings;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Workflows;
using System.IO;
using eforms_middleware.DataModel;
using Newtonsoft.Json;
using System.Linq;

namespace eforms_middleware.MasterData
{
    [DomainAuthorisation]
    public class AllowanceClaimsFunctions
    {

        private readonly AllowanceClaimsApprovalService _allowanceClaimsApprovalService;

        public AllowanceClaimsFunctions(     
        AllowanceClaimsApprovalService allowanceClaimsApprovalService)
        {

            _allowanceClaimsApprovalService = allowanceClaimsApprovalService;
        }

       
        [FunctionName("create-update-additional-hours-claims")]
        public async Task<IActionResult> CreateUpdateAdditionalHoursClaims(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            string additionalhoursclaimsDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var additionalhoursclaimsData = JsonConvert.DeserializeObject<FormInfoRequest>(additionalhoursclaimsDataJson);
            additionalhoursclaimsData.ActionBy = req.Headers["Requesting-User"];
            additionalhoursclaimsData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            return await _allowanceClaimsApprovalService.AdditionalHoursClaimsApproval(additionalhoursclaimsData);
           
        }

        [FunctionName("create-update-casual-timesheets")]
        public async Task<IActionResult> CreateUpdateCasualTimesheets(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            string casualtimesheetsDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var casualtimesheetsData = JsonConvert.DeserializeObject<FormInfoRequest>(casualtimesheetsDataJson);
            casualtimesheetsData.ActionBy = req.Headers["Requesting-User"];
            casualtimesheetsData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            var result = await _allowanceClaimsApprovalService.CasualTimesheetsApproval(casualtimesheetsData);
            return result;
        }
        [FunctionName("create-update-motor-vehicle-allowance-claims")]
        public async Task<IActionResult> CreateUpdateMotorVehicleAllowanceClaims(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string motorvehicleallowanceclaimsDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var motorvehicleallowanceclaimsData = JsonConvert.DeserializeObject<FormInfoRequest>(motorvehicleallowanceclaimsDataJson);
            motorvehicleallowanceclaimsData.ActionBy = req.Headers["Requesting-User"];
            motorvehicleallowanceclaimsData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            var result = await _allowanceClaimsApprovalService.MotorVehicleAllowanceClaimsApproval(motorvehicleallowanceclaimsData);
            return result;
        }
        [FunctionName("create-update-other-allowance-claim")]
        public async Task<IActionResult> CreateUpdateOtherAllowanceClaim(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            string otherallowanceclaimDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var otherallowanceclaimData = JsonConvert.DeserializeObject<FormInfoRequest>(otherallowanceclaimDataJson);
            otherallowanceclaimData.ActionBy = req.Headers["Requesting-User"];
            otherallowanceclaimData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            var result = await _allowanceClaimsApprovalService.OtherAllowanceClaimApproval(otherallowanceclaimData);
            return result;
        }
        [FunctionName("create-update-out-of-hours-contact-claims")]
        public async Task<IActionResult> CreateUpdateOutOfHoursContactClaims(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            string outofhourscontactclaimsDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var outofhourscontactclaimsData = JsonConvert.DeserializeObject<FormInfoRequest>(outofhourscontactclaimsDataJson);
            outofhourscontactclaimsData.ActionBy = req.Headers["Requesting-User"];
            outofhourscontactclaimsData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            var result = await _allowanceClaimsApprovalService.OutOfHoursContactClaimsApproval(outofhourscontactclaimsData);
            return result;
        }
        [FunctionName("create-update-overtime-claims")]
        public async Task<IActionResult> CreateUpdateOvertimeClaims(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            string overtimeclaimsDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var overtimeclaimsData = JsonConvert.DeserializeObject<FormInfoRequest>(overtimeclaimsDataJson);
            overtimeclaimsData.ActionBy = req.Headers["Requesting-User"];
            overtimeclaimsData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            var result = await _allowanceClaimsApprovalService.OvertimeClaimsApproval(overtimeclaimsData);
            return result;
        }
        [FunctionName("create-update-penalty-shift-allowance-claims")]
        public async Task<IActionResult> CreateUpdatePenaltyShiftAllowanceClaims(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string penaltyshiftallowanceclaimsDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var penaltyshiftallowanceclaimsData = JsonConvert.DeserializeObject<FormInfoRequest>(penaltyshiftallowanceclaimsDataJson);
            penaltyshiftallowanceclaimsData.ActionBy = req.Headers["Requesting-User"];
            penaltyshiftallowanceclaimsData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            var result = await _allowanceClaimsApprovalService.PenaltyShiftAllowanceClaimsApproval(penaltyshiftallowanceclaimsData);
            return result;
        }
        [FunctionName("create-update-sea-going-allowance-claim")]
        public async Task<IActionResult> CreateUpdateSeaGoingAllowanceClaim(
          [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
          ILogger log)
        {
            string seagoingallowanceclaimDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var seagoingallowanceclaimData = JsonConvert.DeserializeObject<FormInfoRequest>(seagoingallowanceclaimDataJson);
            seagoingallowanceclaimData.ActionBy = req.Headers["Requesting-User"];
            seagoingallowanceclaimData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            var result = await _allowanceClaimsApprovalService.SeaGoingAllowanceClaimApproval(seagoingallowanceclaimData);
            return result;
        }
    }
}
