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
using Microsoft.Extensions.Configuration;
using eforms_middleware.Settings;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace eforms_middleware.Forms
{
    [DomainAuthorisation]
    public class FormTaskFunctions
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public string createFormTaskSPName = "usp_insert_Form_Task";
        public string updateFormTaskSPName = "usp_update_Form_Task";
        public string getFormTaskByIdSPName = "usp_get_Form_Task_By_Form_Info_ID";
        public string getFormTaskByUserSPName = "usp_get_Form_Task_By_User";
        public string getFormTaskToBeEscalated = "usp_get_Form_Task_To_Be_Escalated";
        public string getFormTaskToBeReminded = "usp_get_Reminders";
        public string updateFormTaskStatusSPName = "usp_update_Form_Task_Status";

        public FormTaskFunctions(IHttpClientFactory _httpClientFactory)
        {
            this._httpClientFactory = _httpClientFactory;
        }

        [FunctionName("func-create-update-form-task-details")]
        public async Task<IActionResult> CreateUpdatFormTask(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-create-update-form-task-details");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var result = new JsonResult(null);

            var tokenProvider = new AzureServiceTokenProvider();
            string accessToken = await tokenProvider.GetAccessTokenAsync("https://database.windows.net");//URI for Azure SQL database

            try
            {
                var formTaskId = CreateUpdateFormTask(requestBody, log, accessToken, context);
                result.Value = new
                {
                    outcome = "Sucess",
                    formItemId = formTaskId
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

        [FunctionName("func-get-form-task-by-user")]
        public async Task<IActionResult> GetAllFormTaskByUser(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-get-form-task-by-user");

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
                var formTask = GetAllFormTaskByUser(userId, log, accessToken, context);
                result.Value = new
                {
                    formTask = formTask,
                    count = formTask.Count
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

        [FunctionName("func-get-form-task-by-form-info-id")]
        public async Task<IActionResult> GetFormTaskByID(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-get-form-task-by-form-info-id");

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
                var formTask = GetFormTaskDetailsByID(formInfoId, log, accessToken, context);
                result.Value = new
                {
                    formTask = formTask,
                    count = formTask.Count
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

        [FunctionName("func-get-reminder-form-task-by-current-date")]
        public async Task<IActionResult> GetReminderFormTaskByCurrentDate(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-get-form-task-by-form-info-id");

            var result = new JsonResult(null);
            var tokenProvider = new AzureServiceTokenProvider();
            string accessToken = await tokenProvider.GetAccessTokenAsync("https://database.windows.net");//URI for Azure SQL database

            try
            {
                var formTask = GetReminderFormTaskByCurrentDate(log, accessToken, context);
                result.Value = new
                {
                    formTask = formTask,
                    count = formTask.Count
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

        internal int CreateUpdateFormTask(string requestBody, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var input = JsonConvert.DeserializeObject<FormTaskInsertModel>(requestBody);
            int newFormTaskId = 0;
            var result = new JsonResult(null);
            var config = new ConfigurationBuilder()
                          .SetBasePath(context.FunctionAppDirectory)
                          .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                          .AddEnvironmentVariables()
                          .Build();

            var cstr = config.GetConnectionString("AZURESQL-ConnectionString");
            var setting1 = config["Setting1"];

            var sqlConnection = cstr;
            try
            {
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    //log.LogInformation($"accessToke: {accessToken}");
                    conn.AccessToken = accessToken;

                    log.LogInformation("02-01-00");
                    //conn.Open();
                    log.LogInformation("03-01");

                    string spName;
                    int? formTaskId;

                    if (input.TaskInfoID == null)
                    {
                        spName = createFormTaskSPName;
                        formTaskId = 0;
                    }
                    else
                    {
                        spName = updateFormTaskSPName;
                        formTaskId = input.TaskInfoID;
                    }

                    using (SqlCommand cmd = new SqlCommand(spName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        //conn.Open();
                        cmd.Parameters.Add("@TaskInfoID", SqlDbType.Int).Value = formTaskId;
                        cmd.Parameters.Add("@AllFormsID", SqlDbType.Int).Value = input.AllFormsID;
                        cmd.Parameters.Add("@FormInfoID", SqlDbType.Int).Value = input.FormInfoID;
                        cmd.Parameters.Add("@EmailInfoID", SqlDbType.Int).Value = input.EmailInfoID;
                        cmd.Parameters.Add("@FormOwnerEmail", SqlDbType.VarChar).Value = input.FormOwnerEmail;
                        cmd.Parameters.Add("@AssignedTo", SqlDbType.VarChar).Value = input.AssignedTo;
                        cmd.Parameters.Add("@TaskStatus", SqlDbType.VarChar).Value = input.TaskStatus;
                        cmd.Parameters.Add("@TaskCreatedDate", SqlDbType.DateTime).Value = input.TaskCreatedDate;
                        cmd.Parameters.Add("@TaskCreatedBy", SqlDbType.VarChar).Value = input.TaskCreatedBy;
                        cmd.Parameters.Add("@TaskCompletedDate", SqlDbType.DateTime).Value = input.TaskCompletedDate;
                        cmd.Parameters.Add("@TaskCompletedBy", SqlDbType.VarChar).Value = input.TaskCompletedBy;
                        cmd.Parameters.Add("@RemindersCount", SqlDbType.Int).Value = input.RemindersCount;
                        cmd.Parameters.Add("@ReminderFrequency", SqlDbType.Int).Value = input.ReminderFrequency;
                        cmd.Parameters.Add("@ReminderTo", SqlDbType.VarChar).Value = input.ReminderTo;
                        cmd.Parameters.Add("@SpecialReminder", SqlDbType.Bit).Value = input.SpecialReminder;
                        cmd.Parameters.Add("@SpecialReminderTo", SqlDbType.VarChar).Value = input.SpecialReminderTo;
                        cmd.Parameters.Add("@SpecialReminderDate", SqlDbType.DateTime).Value = input.SpecialReminderDate;
                        cmd.Parameters.Add("@Escalation", SqlDbType.Bit).Value = input.Escalation;
                        cmd.Parameters.Add("@EscalationDate", SqlDbType.DateTime).Value = input.EscalationDate;
                        cmd.Parameters.Add("@ActiveRecord", SqlDbType.Bit).Value = true;

                        SqlParameter param = new SqlParameter("@TaskInfoIDOUT", SqlDbType.Int);
                        param.Direction = ParameterDirection.InputOutput;
                        param.Value = 0;
                        cmd.Parameters.Add(param);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        log.LogInformation($"New form created successfully. Form Id: {newFormTaskId}");
                        newFormTaskId = int.Parse(cmd.Parameters["@TaskInfoIDOUT"].Value.ToString());

                        conn.Close();
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
            return newFormTaskId;
        }

        internal List<AllFormTaskModel> GetAllFormTaskByUser(string userId, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<AllFormTaskModel> allFormTask = new List<AllFormTaskModel>();
            var result = new JsonResult(null);
            var config = new ConfigurationBuilder()
                          .SetBasePath(context.FunctionAppDirectory)
                          .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                          .AddEnvironmentVariables()
                          .Build();

            var cstr = config.GetConnectionString("AZURESQL-ConnectionString");
            var setting1 = config["Setting1"];
            var sqlConnection = cstr;
            try
            {
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(getFormTaskByIdSPName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EmployeeKey", userId);
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allFormTask.Add(new AllFormTaskModel
                                {
                                    TaskInfoID = reader["TaskInfoID"].ToString(),
                                    AllFormsID = reader["AllFormsID"].ToString(),
                                    FormInfoID = reader["FormInfoID"].ToString(),
                                    EmailInfoID = reader["EmailInfoID"].ToString(),
                                    FormOwnerEmail = reader["FormOwnerEmail"].ToString(),
                                    AssignedTo = reader["AssignedTo"].ToString(),
                                    TaskStatus = reader["TaskStatus"].ToString(),
                                    TaskCreatedDate = reader["TaskCreatedDate"].ToString(),
                                    TaskCreatedBy = reader["TaskCreatedBy"].ToString(),
                                    TaskCompletedDate = reader["TaskCompletedDate"].ToString(),
                                    TaskCompletedBy = reader["TaskCompletedBy"].ToString(),
                                    RemindersCount = reader["RemindersCount"].ToString(),
                                    ReminderFrequency = reader["ReminderFrequency"].ToString(),
                                    ReminderTo = reader["ReminderTo"].ToString(),
                                    SpecialReminder = (bool)reader["SpecialReminder"],
                                    SpecialReminderTo = reader["SpecialReminderTo"].ToString(),
                                    SpecialReminderDate = reader["SpecialReminderDate"].ToString(),
                                    EscalationDate = reader["EscalationDate"].ToString(),
                                    Escalation = (bool)reader["Escalation"],
                                    ActiveRecord = (bool)reader["ActiveRecord"]
                                });
                            }
                        }
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
            return allFormTask;
        }

        internal List<AllFormTaskModel> GetFormTaskDetailsByID(string formInfoId, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<AllFormTaskModel> allFormTask = new List<AllFormTaskModel>();
            var result = new JsonResult(null);
            var config = new ConfigurationBuilder()
                          .SetBasePath(context.FunctionAppDirectory)
                          .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                          .AddEnvironmentVariables()
                          .Build();

            var cstr = config.GetConnectionString("AZURESQL-ConnectionString");
            var setting1 = config["Setting1"];
            var sqlConnection = cstr;
            try
            {
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(getFormTaskByIdSPName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id", formInfoId);
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allFormTask.Add(new AllFormTaskModel
                                {
                                    TaskInfoID = reader["TaskInfoID"].ToString(),
                                    AllFormsID = reader["AllFormsID"].ToString(),
                                    FormInfoID = reader["FormInfoID"].ToString(),
                                    EmailInfoID = reader["EmailInfoID"].ToString(),
                                    FormOwnerEmail = reader["FormOwnerEmail"].ToString(),
                                    AssignedTo = reader["AssignedTo"].ToString(),
                                    TaskStatus = reader["TaskStatus"].ToString(),
                                    TaskCreatedDate = reader["TaskCreatedDate"].ToString(),
                                    TaskCreatedBy = reader["TaskCreatedBy"].ToString(),
                                    TaskCompletedDate = reader["TaskCompletedDate"].ToString(),
                                    TaskCompletedBy = reader["TaskCompletedBy"].ToString(),
                                    RemindersCount = reader["RemindersCount"].ToString(),
                                    ReminderFrequency = reader["ReminderFrequency"].ToString(),
                                    ReminderTo = reader["ReminderTo"].ToString(),
                                    SpecialReminder = (bool)reader["SpecialReminder"],
                                    SpecialReminderTo = reader["SpecialReminderTo"].ToString(),
                                    SpecialReminderDate = reader["SpecialReminderDate"].ToString(),
                                    EscalationDate = reader["EscalationDate"].ToString(),
                                    Escalation = (bool)reader["Escalation"],
                                    ActiveRecord = (bool)reader["ActiveRecord"]
                                });
                            }
                        }
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
            return allFormTask;
        }

        internal List<AllFormTaskModel> GetReminderFormTaskByCurrentDate(ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<AllFormTaskModel> allFormTask = new List<AllFormTaskModel>();
            var result = new JsonResult(null);
            var config = new ConfigurationBuilder()
                          .SetBasePath(context.FunctionAppDirectory)
                          .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                          .AddEnvironmentVariables()
                          .Build();

            var cstr = config.GetConnectionString("AZURESQL-ConnectionString");
            var setting1 = config["Setting1"];
            var sqlConnection = cstr;
            try
            {
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(getFormTaskToBeReminded, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allFormTask.Add(new AllFormTaskModel
                                {
                                    TaskInfoID = reader["TaskInfoID"].ToString(),
                                    AllFormsID = reader["AllFormsID"].ToString(),
                                    FormInfoID = reader["FormInfoID"].ToString(),
                                    EmailInfoID = reader["EmailInfoID"].ToString(),
                                    FormOwnerEmail = reader["FormOwnerEmail"].ToString(),
                                    AssignedTo = reader["AssignedTo"].ToString(),
                                    TaskStatus = reader["TaskStatus"].ToString(),
                                    TaskCreatedDate = reader["TaskCreatedDate"].ToString(),
                                    TaskCreatedBy = reader["TaskCreatedBy"].ToString(),
                                    TaskCompletedDate = reader["TaskCompletedDate"].ToString(),
                                    TaskCompletedBy = reader["TaskCompletedBy"].ToString(),
                                    RemindersCount = reader["RemindersCount"].ToString(),
                                    ReminderFrequency = reader["ReminderFrequency"].ToString(),
                                    ReminderTo = reader["ReminderTo"].ToString(),
                                    SpecialReminder = (bool)reader["SpecialReminder"],
                                    SpecialReminderTo = reader["SpecialReminderTo"].ToString(),
                                    SpecialReminderDate = reader["SpecialReminderDate"].ToString(),
                                    EscalationDate = reader["EscalationDate"].ToString(),
                                    Escalation = (bool)reader["Escalation"],
                                    ActiveRecord = (bool)reader["ActiveRecord"]
                                });
                            }
                        }
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
            return allFormTask;
        }

    }

}
