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
using eforms_middleware.Settings;

namespace eforms_middleware.Forms
{
    [DomainAuthorisation]
    public static class RefFormStatusFunctions
    {
        public static string createRefFormStatusSPName = "usp_insert_Forms_Status_Info";
        public static string updateRefFormStatusSPName = "usp_update_Forms_Status_Info";
        public static string getRefFormStatusByIdSPName = "usp_get_Ref_Form_Status_By_Status_Or_ID";
        public static string getRefFormStatusByUserSPName = "usp_get_All_Form_Status_By_User";

        [FunctionName("func-create-update-ref-form-status")]
        public static async Task<IActionResult> CreateUpdatRefFormStatus(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-create-update-form-details");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var result = new JsonResult(null);
            var tokenProvider = new AzureServiceTokenProvider();
            string accessToken = await tokenProvider.GetAccessTokenAsync("https://database.windows.net");//URI for Azure SQL database

            try
            {
                var refFormStatusItemId = CreateUpdateRefFormStatus(requestBody, log, accessToken, context);
                result.Value = new
                {
                    outcome = "Sucess",
                    formItemId = refFormStatusItemId
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

        [FunctionName("func-get-ref-form-status-by-user")]
        public static async Task<IActionResult> GetRefFormStatusByUser(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {

            var result = new JsonResult(null);

            string userId = req.Query["user"];
            if (string.IsNullOrEmpty(userId))
            {
                result.Value = new
                {
                    error = "No user supplied"
                };
                result.StatusCode = StatusCodes.Status403Forbidden;
                return result;
            }

            var tokenProvider = new AzureServiceTokenProvider();
            string accessToken = await tokenProvider.GetAccessTokenAsync("https://database.windows.net");//URI for Azure SQL database

            try
            {
                var refFormStatus = GetRefFormStatus(userId, log, accessToken, context);
                result.Value = new
                {
                    allForms = refFormStatus,
                    count = refFormStatus.Count
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

        [FunctionName("func-get-ref-form-status-by-id")]
        public static async Task<IActionResult> GetRefFormStatusByID(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            var result = new JsonResult(null);

            string refFormStatusId = req.Query["Id"];
            if (string.IsNullOrEmpty(refFormStatusId))
            {
                result.Value = new
                {
                    error = "No form Id supplied"
                };
                result.StatusCode = StatusCodes.Status403Forbidden;
                return result;
            }

            var tokenProvider = new AzureServiceTokenProvider();
            string accessToken = await tokenProvider.GetAccessTokenAsync("https://database.windows.net");//URI for Azure SQL database

            try
            {
                var refFormStatus = GetRefFormStatusDetailsByID(refFormStatusId, log, accessToken, context);
                result.Value = new
                {
                    refFormStatus = refFormStatus,
                    count = refFormStatus.Count
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

        internal static int CreateUpdateRefFormStatus(string requestBody, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var input = JsonConvert.DeserializeObject<RefFormStatusInsertModel>(requestBody);
            int newRefFormStatusItemId = 0;

            var result = new JsonResult(null);

            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;

                    string spName;
                    int? refFormStatusID;

                    if (input.RefStatusesID == null)
                    {
                        spName = createRefFormStatusSPName;
                        refFormStatusID = 0;
                    }
                    else
                    {
                        spName = updateRefFormStatusSPName;
                        refFormStatusID = input.RefStatusesID;
                    }

                    using (SqlCommand cmd = new SqlCommand(spName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@RefStatusesID", SqlDbType.Int).Value = refFormStatusID;
                        cmd.Parameters.Add("@Directorate", SqlDbType.VarChar).Value = input.Status;
                        //cmd.Parameters.Add("@ActiveRecord", SqlDbType.VarChar).Value = input.ActiveRecord;

                        SqlParameter param = new SqlParameter("@RefFormStatusItemIDOUT", SqlDbType.Int);
                        param.Direction = ParameterDirection.InputOutput;
                        param.Value = 0;
                        cmd.Parameters.Add(param);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        log.LogInformation($"New form created successfully. All Form Id: {newRefFormStatusItemId}");
                        newRefFormStatusItemId = int.Parse(cmd.Parameters["@RefFormStatusItemIDOUT"].Value.ToString());

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
            return newRefFormStatusItemId;
        }

        internal static List<RefFormStatusGetModel> GetRefFormStatus(string userId, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<RefFormStatusGetModel> allRefFormStatus = new List<RefFormStatusGetModel>();
            var result = new JsonResult(null);

            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(getRefFormStatusByUserSPName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EmployeeKey", userId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allRefFormStatus.Add(new RefFormStatusGetModel
                                {
                                    RefStatusesID = int.Parse(reader["RefStatusesID"].ToString()),
                                    Status = reader["Status"].ToString(),
                                    //ActiveRecord = (bool)reader["ActiveRecord"],
                                });
                            }
                        }
                        // conn.Close();
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
            return allRefFormStatus;
        }

        internal static List<RefFormStatusGetModel> GetRefFormStatusDetailsByID(string refFormStatusId, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<RefFormStatusGetModel> allRefFormStatus = new List<RefFormStatusGetModel>();
            var result = new JsonResult(null);
            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;

                    using (SqlCommand cmd = new SqlCommand(getRefFormStatusByIdSPName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ID", refFormStatusId);

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allRefFormStatus.Add(new RefFormStatusGetModel
                                {
                                    RefStatusesID = int.Parse(reader["RefStatusesID"].ToString()),
                                    Status = reader["Status"].ToString(),
                                    //ActiveRecord = (bool)reader["ActiveRecord"],
                                });
                            }
                        }
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
            return allRefFormStatus;
        }
    }
}
