using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using static eforms_middleware.Settings.Helper;
using System.Linq;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace eforms_middleware.Settings
{
    public class DomainAuthorisationAttribute : FunctionInvocationFilterAttribute
    {
        public override Task OnExecutingAsync(FunctionExecutingContext executingContext
            , CancellationToken cancellationToken)
        {
#if (!DEBUG)
            HttpRequest request = executingContext.Arguments.First().Value as HttpRequest;

            string userId = request.Headers["Requesting-User"];

            if (request.Headers.ContainsKey("upn"))
            {                
                var upn = request.Headers["upn"]; 

                if (CurrentDomain == DomainType.PRD)
                {
                    if (userId != upn)
                    {
                        RunException(request);
                    }
                }
                else
                {
                    if (!IsImpersonationAllowed)
                    {
                        RunException(request);
                    }
                }
            }
            else
            {
                RunException(request);
            }
#endif
            return Task.CompletedTask;
        }

        private void RunException(HttpRequest request)
        {
            request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            request.HttpContext.Response.Body.FlushAsync();
            request.HttpContext.Response.Body.Close();
            throw new UnauthorizedAccessException("403: Forbidden");
        }
    }
}