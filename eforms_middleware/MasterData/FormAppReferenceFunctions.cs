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
    public static class FormAppReferenceFunctions
    {
        public static string createFormAppReferenceSPName = "usp_insert_Form_AppReference";
        public static string updateFormAppReferenceSPName = "usp_update_Form_AppReference";
        public static string getFormAppReferenceByIdSPName = "usp_get_Form_AppReference_By_ID";
        public static string getFormAppReferenceByUserSPName = "usp_get_Form_AppReference_By_User";

        [FunctionName("func-create-update-form-appreference-details")]
        public static async Task<IActionResult> CreateUpdatFormAppReference(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-create-update-form-appreference-details");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var result = new JsonResult(null);

            var tokenProvider = new AzureServiceTokenProvider();
            string accessToken = await tokenProvider.GetAccessTokenAsync("https://database.windows.net");//URI for Azure SQL database

            try
            {
                var formAppReferenceId = CreateUpdateFormAppReference(requestBody, log, accessToken, context);
                result.Value = new
                {
                    outcome = "Sucess",
                    formAppReferenceId = formAppReferenceId
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
            log.LogInformation("END - C# HTTP trigger function processed a request for Function App: func-create-update-form-appreference-details");
            return result;
        }

        [FunctionName("func-get-form-appreference-by-user")]
        public static async Task<IActionResult> GetAllFormAppReferenceByUser(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-get-form-appreference-by-user");

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
                var formAppReference = GetAllFormAppReferenceByUser(userId, log, accessToken, context);
                result.Value = new
                {
                    formAppReference = formAppReference,
                    count = formAppReference.Count
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
            log.LogInformation("END - C# HTTP trigger function processed a request for Function App: func-get-form-appreference-by-user");

            return result;
        }

        [FunctionName("func-get-form-appreference-by-form-info-id")]
        public static async Task<IActionResult> GetFormAppReferenceByID(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-get-form-appreference-by-form-info-id");

            var result = new JsonResult(null);

            string formInfoId = req.Query["Id"];
            if (string.IsNullOrEmpty(formInfoId))
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
                var formAppReference = GetFormAppReferenceDetailsByID(formInfoId, log, accessToken, context);
                result.Value = new
                {
                    formAppReference = formAppReference,
                    count = formAppReference.Count
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
            log.LogInformation("END - C# HTTP trigger function processed a request for Function App: func-get-form-appreference-by-form-info-id");

            return result;
        }

        internal static int CreateUpdateFormAppReference(string requestBody, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var input = JsonConvert.DeserializeObject<FormAppReferenceInsertModel>(requestBody);
            int newFormAppReferenceId = 0;

            var result = new JsonResult(null);
            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;
                    string spName;
                    int? formAppReferenceId;

                    if (input.FormAppReferenceID == null)
                    {
                        spName = createFormAppReferenceSPName;
                        formAppReferenceId = 0;
                    }
                    else
                    {
                        spName = updateFormAppReferenceSPName;
                        formAppReferenceId = input.FormAppReferenceID;
                    }

                    using (SqlCommand cmd = new SqlCommand(spName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@FormAppReferenceID", SqlDbType.Int).Value = formAppReferenceId;
                        cmd.Parameters.Add("@AllFormsID", SqlDbType.Int).Value = input.AllFormsID;
                        cmd.Parameters.Add("@LogicAppID", SqlDbType.VarChar).Value = input.LogicAppID;
                        cmd.Parameters.Add("@LogicAppFor", SqlDbType.VarChar).Value = input.LogicAppFor;
                        cmd.Parameters.Add("@FunctionAppID", SqlDbType.VarChar).Value = input.FunctionAppID;
                        cmd.Parameters.Add("@FunctionAppFor", SqlDbType.VarChar).Value = input.FunctionAppFor;
                        cmd.Parameters.Add("@Modified", SqlDbType.DateTime).Value = input.Modified;
                        cmd.Parameters.Add("@ModifiedBy", SqlDbType.VarChar).Value = input.ModifiedBy;
                        cmd.Parameters.Add("@ActiveRecord", SqlDbType.Bit).Value = true;

                        SqlParameter param = new SqlParameter("@FormAppReferenceIDOUT", SqlDbType.Int);
                        param.Direction = ParameterDirection.InputOutput;
                        param.Value = 0;
                        cmd.Parameters.Add(param);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        log.LogInformation($"New form created successfully. Form Id: {newFormAppReferenceId}");
                        newFormAppReferenceId = int.Parse(cmd.Parameters["@FormAppReferenceIDOUT"].Value.ToString());

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
            return newFormAppReferenceId;
        }

        internal static List<FormAppReferenceGetModel> GetAllFormAppReferenceByUser(string userId, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<FormAppReferenceGetModel> allFormAppReference = new List<FormAppReferenceGetModel>();
            var result = new JsonResult(null);
            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(getFormAppReferenceByUserSPName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EmployeeKey", userId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allFormAppReference.Add(new FormAppReferenceGetModel
                                {
                                    FormAppReferenceID = reader["FormAppReferenceID"].ToString(),
                                    AllFormsID = reader["AllFormsID"].ToString(),
                                    LogicAppID = reader["LogicAppID"].ToString(),
                                    LogicAppFor = reader["LogicAppFor"].ToString(),
                                    FunctionAppID = reader["FunctionAppID"].ToString(),
                                    FunctionAppFor = reader["FunctionAppFor"].ToString(),
                                    Modified = reader["Modified"].ToString(),
                                    ModifiedBy = reader["ModifiedBy"].ToString(),
                                    ActiveRecord = (bool)reader["ActiveRecord"]
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
            return allFormAppReference;
        }

        internal static List<FormAppReferenceGetModel> GetFormAppReferenceDetailsByID(string formInfoId, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<FormAppReferenceGetModel> allFormAppReference = new List<FormAppReferenceGetModel>();
            var result = new JsonResult(null);
            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(getFormAppReferenceByIdSPName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id", formInfoId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allFormAppReference.Add(new FormAppReferenceGetModel
                                {
                                    FormAppReferenceID = reader["FormAppReferenceID"].ToString(),
                                    AllFormsID = reader["AllFormsID"].ToString(),
                                    LogicAppID = reader["LogicAppID"].ToString(),
                                    LogicAppFor = reader["LogicAppFor"].ToString(),
                                    FunctionAppID = reader["FunctionAppID"].ToString(),
                                    FunctionAppFor = reader["FunctionAppFor"].ToString(),
                                    Modified = reader["Modified"].ToString(),
                                    ModifiedBy = reader["ModifiedBy"].ToString(),
                                    ActiveRecord = (bool)reader["ActiveRecord"]
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
            return allFormAppReference;
        }
    }
}
