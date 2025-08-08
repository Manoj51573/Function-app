using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Services.AppAuthentication;
using System.Data;
using Microsoft.Data.SqlClient;
using eforms_middleware.DataModel;
using System.Collections.Generic;
using System.IO;
using eforms_middleware.Forms;
using eforms_middleware.Interfaces;
using eforms_middleware.Settings;

namespace eforms_middleware.MasterData
{
    [DomainAuthorisation]
    public class FormHistoryFunctions
    {
        private readonly IFormHistoryService _formHistoryService;
        public static string createFormHistorySPName = "usp_insert_Form_History";
        public static string updateFormHistorySPName = "usp_update_Form_History";
        public static string getFormHistoryByIdSPName = "usp_get_Form_History_By_Form_Info_ID";

        public FormHistoryFunctions(IFormHistoryService formHistoryService)
        {
            _formHistoryService = formHistoryService;
        }

        [FunctionName("func-create-update-form-history-details")]
        public static async Task<IActionResult> CreateUpdatFormHistory(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-create-update-form-history-details");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var result = new JsonResult(null);

            var tokenProvider = new AzureServiceTokenProvider();
            string accessToken = await tokenProvider.GetAccessTokenAsync("https://database.windows.net");//URI for Azure SQL database

            try
            {
                var formHistoryId = CreateUpdateFormHistory(requestBody, log, accessToken, context);
                result.Value = new
                {
                    outcome = "Sucess",
                    formHistoryItemId = formHistoryId
                };
                result.StatusCode = StatusCodes.Status200OK;
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                result.Value = new
                {
                    error = e.Message
                };
                result.StatusCode = StatusCodes.Status500InternalServerError;
            }

            return result;
        }

        [FunctionName("func-get-form-history-by-form-info-id")]
        public async Task<IActionResult> GetFormHistoryByID(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            var result = new JsonResult(null);
            
            if (!int.TryParse(req.Query["Id"], out int formInfoId))
            {
                result.Value = new
                {
                    error = "No form Id supplied"
                };
                result.StatusCode = StatusCodes.Status400BadRequest;
                return result;
            }

            try
            {
                var formHistory = await _formHistoryService.GetFormHistoryDetailsByID(formInfoId);
                result.Value = new
                {
                    formHistory,
                    count = formHistory.Count
                };
                result.StatusCode = StatusCodes.Status200OK;
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                result.Value = new
                {
                    error = e.Message
                };
                result.StatusCode = StatusCodes.Status500InternalServerError;
            }

            return result;
        }

        internal static int CreateUpdateFormHistory(string requestBody, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var input = JsonConvert.DeserializeObject<FormHistoryInsertModel>(requestBody);
            int newFormHistoryId = 0;

            var result = new JsonResult(null);

            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;

                    string spName;
                    int? formHistoryId;

                    if (input.FormHistoryID == null)
                    {
                        spName = createFormHistorySPName;
                        formHistoryId = 0;
                    }
                    else
                    {
                        spName = updateFormHistorySPName;
                        formHistoryId = input.FormHistoryID;
                    }

                    using (SqlCommand cmd = new SqlCommand(spName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        //conn.Open();
                        cmd.Parameters.Add("@FormHistoryID", SqlDbType.Int).Value = formHistoryId;
                        cmd.Parameters.Add("@AllFormsID", SqlDbType.Int).Value = input.AllFormsID;
                        cmd.Parameters.Add("@FormInfoID", SqlDbType.Int).Value = input.FormInfoID;
                        cmd.Parameters.Add("@Created", SqlDbType.DateTime).Value = input.Created;
                        if (string.IsNullOrEmpty(input.ActionType))
                        {
                            cmd.Parameters.Add("@ActionType", SqlDbType.VarChar).Value = "";
                        }
                        else
                        {
                            cmd.Parameters.Add("@ActionType", SqlDbType.VarChar).Value = input.ActionType;
                        }
                        if (string.IsNullOrEmpty(input.ActionBy))
                        {
                            cmd.Parameters.Add("@ActionBy", SqlDbType.VarChar).Value = "";
                        }
                        else
                        {
                            cmd.Parameters.Add("@ActionBy", SqlDbType.VarChar).Value = input.ActionBy;
                        }
                        //Get Form Status ID by calling a FA.
                        RefFormStatusGetModel status = getFormStatusValue(input.FormStatusID, log, accessToken, context);
                        cmd.Parameters.Add("@FormStatusID", SqlDbType.Int).Value = status.RefStatusesID;

                        if (string.IsNullOrEmpty(input.AditionalComments))
                        {
                            cmd.Parameters.Add("@AditionalComments", SqlDbType.VarChar).Value = "";
                        }
                        else
                        {
                            cmd.Parameters.Add("@AditionalComments", SqlDbType.VarChar).Value = input.AditionalComments;
                        }
                        cmd.Parameters.Add("@ActiveRecord", SqlDbType.Bit).Value = true;

                        SqlParameter param = new SqlParameter("@FormHistoryIDOUT", SqlDbType.Int);
                        param.Direction = ParameterDirection.InputOutput;
                        param.Value = 0;
                        cmd.Parameters.Add(param);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        log.LogInformation($"New form created successfully. Form Id: {newFormHistoryId}");
                        newFormHistoryId = int.Parse(cmd.Parameters["@FormHistoryIDOUT"].Value.ToString());

                        //conn.Close();
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                result.Value = new
                {
                    error = e.Message
                };
                result.StatusCode = StatusCodes.Status500InternalServerError;
            }
            return newFormHistoryId;
        }

        internal static RefFormStatusGetModel getFormStatusValue(string Id, ILogger log, string accessToken, ExecutionContext context)
        {
            try
            {
                List<RefFormStatusGetModel> allRefFormStatus = new List<RefFormStatusGetModel>();
                allRefFormStatus = RefFormStatusFunctions.GetRefFormStatusDetailsByID(Id, log, accessToken, context);
                foreach (RefFormStatusGetModel item in allRefFormStatus)
                {
                    return item;
                }
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
            }
            return null;
        }

    }
}
