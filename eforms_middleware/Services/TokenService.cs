using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;

namespace eforms_middleware.Services
{
    public class TokenService
    {
        private string _existingToken;
        
        public async Task<string> GetToken()
        {
            if (string.IsNullOrEmpty(_existingToken))
            {
                var tokenProvider = new AzureServiceTokenProvider();
                _existingToken = await tokenProvider.GetAccessTokenAsync("https://database.windows.net");//URI for Azure SQL database
            }

            return _existingToken;
        }
    }
}