using Microsoft.Azure.KeyVault;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;
using Microsoft.Azure.Services.AppAuthentication;

namespace eforms_middleware.InternalSystem
{
    public class GraphCoreConnector
    {
        private string ClientID;
        private X509Certificate2 PFxCertificate;
        private string TenantId;
        private KeyVaultClient keyVaultClient;
        public GraphCoreConnector(string clientId, string tenantId, X509Certificate2 certificate)
        {
            this.ClientID = clientId;
            this.PFxCertificate = certificate;
            this.TenantId = tenantId;
        }

        public GraphCoreConnector(string clientId, string tenantId, string keyvaultName, string certificateName)
        {
            this.ClientID = clientId;
            this.PFxCertificate = GetKeyVaultCertificate(keyvaultName, certificateName);
            this.TenantId = tenantId;
        }
        public GraphServiceClient GetAuthenticatedGraphClient()
        {
            var confidentialClient = this.CreateAuthorizationProvider();

            GraphServiceClient graphServiceClient = new GraphServiceClient(confidentialClient);
            //new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) => {
            //    var scopes = new string[] { "https://graph.microsoft.com/.default" };
            //    // Retrieve an access token for Microsoft Graph (gets a fresh token if needed).
            //    var authResult = await confidentialClient.AcquireTokenForClient(scopes).ExecuteAsync();

            //    // Add the access token in the Authorization header of the API
            //    requestMessage.Headers.Authorization =
            //    new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

            //})
            //);
            return graphServiceClient;
        }
        internal X509Certificate2 GetKeyVaultCertificate(string keyvaultName, string name)
        {
            // Some steps need to be taken to make this work
            // 1. Create a KeyVault and upload the certificate
            // 2. Give the Function App the permission to GET certificates via Access Policies in the KeyVault
            // 3. Call an explicit access token request to the management resource to https://vault.azure.net and use the URL of our Keyvault in the GetSecretMethod
            if (this.keyVaultClient == null)
            {
                // this token provider gets the appid/secret from the azure function identity
                // and thus makes the call on behalf of that appid/secret
                var serviceTokenProvider = new AzureServiceTokenProvider();
                keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(serviceTokenProvider.KeyVaultTokenCallback));
            }

            // Getting the certificate
            var secret = keyVaultClient.GetSecretAsync("https://" + keyvaultName + ".vault.azure.net/", name);

            // Returning the certificate
            return new X509Certificate2(Convert.FromBase64String(secret.Result.Value));

            // If you receive the following error when running the Function; 
            // Microsoft.Azure.WebJobs.Host.FunctionInvocationException: 
            // Exception while executing function: NotificationFunctions.QueueOperation--->
            // System.Security.Cryptography.CryptographicException: 
            // The system cannot find the file specified.at System.Security.Cryptography.NCryptNative.ImportKey(SafeNCryptProviderHandle provider, Byte[] keyBlob, String format) at System.Security.Cryptography.CngKey.Import(Byte[] keyBlob, CngKeyBlobFormat format, CngProvider provider)
            // 
            // Please see https://stackoverflow.com/questions/31685278/create-a-self-signed-certificate-in-net-using-an-azure-web-application-asp-ne
            // Add the following Application setting to the AF "WEBSITE_LOAD_USER_PROFILE = 1"
        }
        private ClientCredentialProvider CreateAuthorizationProvider()
        {

            var authority = $"https://login.microsoftonline.com/{this.TenantId}/v2.0";
            //this specific scope means that application will default to what is defined in the application registration rather than using dynamic scopes

            IConfidentialClientApplication clientApp = ConfidentialClientApplicationBuilder
                                            .Create(this.ClientID)
                                            .WithCertificate(this.PFxCertificate)
                                            .WithAuthority(authority)
                                            .Build();
            //return clientApp;
            ClientCredentialProvider authProvider = new ClientCredentialProvider(clientApp);
            return authProvider;
        }
    }
}