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
    public static class RefDirectoratesFunctions
    {
        public static string createRefDirectoratesSPName = "usp_insert_All_Forms_Info";
        public static string updateRefDirectoratesSPName = "usp_update_All_Forms_Info";
        public static string getRefDirectoratesByIdSPName = "usp_get_Ref_Directorates_By_Directorate_Or_ID";
        public static string getRefDirectoratesByUserSPName = "usp_get_All_Form_By_User";

        [FunctionName("func-create-update-ref-directorates")]
        public static async Task<IActionResult> CreateUpdatRefDirectorates(
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
                var refDirectoratesItemId = CreateUpdateRefDirectorates(requestBody, log, accessToken, context);
                result.Value = new
                {
                    outcome = "Sucess",
                    formItemId = refDirectoratesItemId
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

        [FunctionName("func-get-ref-directorates-by-user")]
        public static async Task<IActionResult> GetRefDirectoratesByUser(
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
                var refDirectorates = GetRefDirectorates(userId, log, accessToken, context);
                result.Value = new
                {
                    allForms = refDirectorates,
                    count = refDirectorates.Count
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

        [FunctionName("func-get-ref-directorates-by-id")]
        public static async Task<IActionResult> GetRefDirectoratesByID(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            var result = new JsonResult(null);

            string refDirectoratesId = req.Query["Id"];
            if (string.IsNullOrEmpty(refDirectoratesId))
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
                var refDirectorates = GetRefDirectoratesDetailsByID(refDirectoratesId, log, accessToken, context);
                result.Value = new
                {
                    refDirectorates = refDirectorates,
                    count = refDirectorates.Count
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

        internal static int CreateUpdateRefDirectorates(string requestBody, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var input = JsonConvert.DeserializeObject<RefDirectoratesInsertModel>(requestBody);
            int newRefDirectoratesItemId = 0;

            var result = new JsonResult(null);

            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;

                    string spName;
                    int? refDirectoratesID;

                    if (input.RefDirectoratesID == null)
                    {
                        spName = createRefDirectoratesSPName;
                        refDirectoratesID = 0;
                    }
                    else
                    {
                        spName = updateRefDirectoratesSPName;
                        refDirectoratesID = input.RefDirectoratesID;
                    }

                    using (SqlCommand cmd = new SqlCommand(spName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@RefDirectoratesID", SqlDbType.Int).Value = refDirectoratesID;
                        cmd.Parameters.Add("@Directorate", SqlDbType.VarChar).Value = input.Directorate;
                        //cmd.Parameters.Add("@ActiveRecord", SqlDbType.VarChar).Value = input.ActiveRecord;

                        SqlParameter param = new SqlParameter("@RefDirectoratesItemIDOUT", SqlDbType.Int);
                        param.Direction = ParameterDirection.InputOutput;
                        param.Value = 0;
                        cmd.Parameters.Add(param);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        log.LogInformation($"New directorate created successfully. All Form Id: {newRefDirectoratesItemId}");
                        newRefDirectoratesItemId = int.Parse(cmd.Parameters["@RefDirectoratesItemIDOUT"].Value.ToString());

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
            return newRefDirectoratesItemId;
        }

        internal static List<RefDirectoratesGetModel> GetRefDirectorates(string userId, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<RefDirectoratesGetModel> allRefDirectorates = new List<RefDirectoratesGetModel>();
            var result = new JsonResult(null);

            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(getRefDirectoratesByUserSPName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EmployeeKey", userId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allRefDirectorates.Add(new RefDirectoratesGetModel
                                {
                                    RefDirectoratesID = int.Parse(reader["RefDirectoratesID"].ToString()),
                                    Directorate = reader["Directorate"].ToString(),
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
            return allRefDirectorates;
        }

        internal static List<RefDirectoratesGetModel> GetRefDirectoratesDetailsByID(string refDirectoratesId, ILogger log, string accessToken, ExecutionContext context)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<RefDirectoratesGetModel> allRefDirectorates = new List<RefDirectoratesGetModel>();
            var result = new JsonResult(null);

            try
            {
                string sqlConnection = StaticMethods.AzureSQLConnectionString(context);
                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.AccessToken = accessToken;

                    using (SqlCommand cmd = new SqlCommand(getRefDirectoratesByIdSPName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ID", refDirectoratesId);

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allRefDirectorates.Add(new RefDirectoratesGetModel
                                {
                                    RefDirectoratesID = int.Parse(reader["RefDirectoratesID"].ToString()),
                                    Directorate = reader["Directorate"].ToString(),
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
            return allRefDirectorates;
        }
    }
}
