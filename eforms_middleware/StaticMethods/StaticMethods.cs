using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace eforms_middleware
{
    public static class StaticMethods
    {
        public static string AzureSQLConnectionString(ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                            .SetBasePath(context.FunctionAppDirectory)
                            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables()
                            .Build();
            var cstr = config.GetConnectionString("AZURESQL-ConnectionString");

            return $"{cstr}";
        }

        public static string AzureADSecret(ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                            .SetBasePath(context.FunctionAppDirectory)
                            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables()
                            .Build();
            var azureADsecret = config.GetConnectionString("AZUREAD-Secret");

            return $"{azureADsecret}";
        }
    }
}
