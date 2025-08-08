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
    public static class FormEmailFunctions
    {
        public static string createFormEmailSPName = "usp_insert_Form_Email";
        public static string updateFormEmailSPName = "usp_update_Form_Email";
        public static string getFormEmailByIdSPName = "usp_get_Form_Email_By_Form_Info_ID";
        public static string getFormEmailByUserSPName = "usp_get_Form_Email_By_User";

        [FunctionName("func-create-update-form-email-details")]
        public static async Task<IActionResult> CreateUpdatFormEmail(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-create-update-form-email-details");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var result = new JsonResult(null);

            var tokenProvider = new AzureServiceTokenProvider();
            string accessToken = await tokenProvider.GetAccessTokenAsync("https://database.windows.net");//URI for Azure SQL database

            try
            {
                var formEmailId = CreateUpdateFormEmail(requestBody, log, accessToken, context);
                result.Value = new
                {
                    outcome = "Sucess",
                    formItemId = formEmailId
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
            log.LogInformation("END - C# HTTP trigger function processed a request for Function App: func-create-update-form-email-details");

            return result;
        }

        [FunctionName("func-get-form-email-by-user")]
        public static async Task<IActionResult> GetAllFormEmailByUser(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-get-form-email-by-user");

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
                var formEmail = GetAllFormEmailByUser(userId, log, accessToken, context);
                result.Value = new
                {
                    formEmail = formEmail,
                    count = formEmail.Count
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
            log.LogInformation("END - C# HTTP trigger function processed a request for Function App: func-get-form-email-by-user");

            return result;
        }

        [FunctionName("func-get-form-email-by-form-info-id")]
        public static async Task<IActionResult> GetFormEmailByID(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-get-form-email-by-form-info-id");

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
                var formEmail = GetFormEmailDetailsByID(formInfoId, log, accessToken, context);
                result.Value = new
                {
                    formEmail = formEmail,
                    count = formEmail.Count
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
            log.LogInformation("END - C# HTTP trigger function processed a request for Function App: func-get-form-email-by-form-info-id");

            return result;
        }

        internal static int CreateUpdateFormEmail(string requestBody, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var input = JsonConvert.DeserializeObject<FormEmailInsertModel>(requestBody);
            int newFormEmailId = 0;
            var result = new JsonResult(null);

            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;
                    string spName;
                    int? formEmailId;

                    if (input.EmailInfoID == null)
                    {
                        spName = createFormEmailSPName;
                        formEmailId = 0;
                    }
                    else
                    {
                        spName = updateFormEmailSPName;
                        formEmailId = input.EmailInfoID;
                    }

                    using (SqlCommand cmd = new SqlCommand(spName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@EmailInfoID", SqlDbType.Int).Value = formEmailId;
                        cmd.Parameters.Add("@AllFormsID", SqlDbType.Int).Value = input.AllFormsID;
                        cmd.Parameters.Add("@EmailReferenceTitle", SqlDbType.VarChar).Value = input.EmailReferenceTitle;
                        cmd.Parameters.Add("@EmailSubject", SqlDbType.VarChar).Value = input.EmailSubject;
                        cmd.Parameters.Add("@EmailSequence", SqlDbType.VarChar).Value = input.EmailSequence;
                        cmd.Parameters.Add("@EmailFrom", SqlDbType.VarChar).Value = input.EmailFrom;
                        cmd.Parameters.Add("@EmailTo", SqlDbType.VarChar).Value = input.EmailTo;
                        cmd.Parameters.Add("@EmailCC", SqlDbType.VarChar).Value = input.EmailCC;
                        cmd.Parameters.Add("@EmailBCC", SqlDbType.VarChar).Value = input.EmailBCC;
                        cmd.Parameters.Add("@EmailContent", SqlDbType.VarChar).Value = input.EmailContent;
                        cmd.Parameters.Add("@Modified", SqlDbType.VarChar).Value = input.Modified;
                        cmd.Parameters.Add("@ModifiedBy", SqlDbType.VarChar).Value = input.ModifiedBy;
                        cmd.Parameters.Add("@EmailHeader", SqlDbType.VarChar).Value = input.EmailHeader;
                        cmd.Parameters.Add("@EmailFooter", SqlDbType.VarChar).Value = input.EmailFooter;
                        cmd.Parameters.Add("@TaskTo", SqlDbType.VarChar).Value = input.TaskTo;
                        cmd.Parameters.Add("@ActiveRecord", SqlDbType.Bit).Value = true;

                        SqlParameter param = new SqlParameter("@FormEmailIDOUT", SqlDbType.Int);
                        param.Direction = ParameterDirection.InputOutput;
                        param.Value = 0;
                        cmd.Parameters.Add(param);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        log.LogInformation($"New form created successfully. Form Id: {newFormEmailId}");
                        newFormEmailId = int.Parse(cmd.Parameters["@FormEmailIDOUT"].Value.ToString());

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
            return newFormEmailId;
        }

        internal static List<AllFormEmailModel> GetAllFormEmailByUser(string userId, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<AllFormEmailModel> allFormEmail = new List<AllFormEmailModel>();
            var result = new JsonResult(null);
            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    //log.LogInformation($"accessToke: {accessToken}");
                    conn.AccessToken = accessToken;
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(getFormEmailByIdSPName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EmployeeKey", userId);
                        //conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allFormEmail.Add(new AllFormEmailModel
                                {
                                    EmailInfoID = (int)reader["EmailInfoID"],
                                    AllFormsID = (int)reader["AllFormsID"],
                                    EmailReferenceTitle = (string)reader["EmailReferenceTitle"],
                                    EmailSubject = (string)reader["EmailSubject"],
                                    EmailSequence = (int)reader["EmailSequence"],
                                    EmailFrom = (string)reader["EmailFrom"],
                                    EmailTo = (string)reader["EmailTo"],
                                    EmailCC = (string)reader["EmailCC"],
                                    EmailBCC = (string)reader["EmailBCC"],
                                    EmailContent = (string)reader["EmailContent"],
                                    Modified = (DateTime)reader["Modified"],
                                    ModifiedBy = (string)reader["ModifiedBy"],
                                    EmailHeader = (string)reader["EmailHeader"],
                                    EmailFooter = (string)reader["EmailFooter"],
                                    TaskTo = (string)reader["TaskTo"],
                                    ActiveRecord = (bool)reader["ActiveRecord"]
                                });
                            }
                        }
                    }
                    //conn.Close();
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
            return allFormEmail;
        }

        internal static List<AllFormEmailModel> GetFormEmailDetailsByID(string formInfoId, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<AllFormEmailModel> allFormEmail = new List<AllFormEmailModel>();
            var result = new JsonResult(null);
            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(getFormEmailByIdSPName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id", formInfoId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allFormEmail.Add(new AllFormEmailModel
                                {
                                    EmailInfoID = (int)reader["EmailInfoID"],
                                    AllFormsID = (int)reader["AllFormsID"],
                                    EmailReferenceTitle = (string)reader["EmailReferenceTitle"],
                                    EmailSubject = (string)reader["EmailSubject"],
                                    EmailSequence = (int)reader["EmailSequence"],
                                    EmailFrom = (string)reader["EmailFrom"],
                                    EmailTo = (string)reader["EmailTo"],
                                    EmailCC = (string)reader["EmailCC"],
                                    EmailBCC = (string)reader["EmailBCC"],
                                    EmailContent = (string)reader["EmailContent"],
                                    Modified = (DateTime)reader["Modified"],
                                    ModifiedBy = (string)reader["ModifiedBy"],
                                    EmailHeader = (string)reader["EmailHeader"],
                                    EmailFooter = (string)reader["EmailFooter"],
                                    TaskTo = (string)reader["TaskTo"],
                                    ActiveRecord = (bool)reader["ActiveRecord"]
                                });
                            }
                        }
                    }
                    //conn.Close();
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
            return allFormEmail;
        }
    }
}
