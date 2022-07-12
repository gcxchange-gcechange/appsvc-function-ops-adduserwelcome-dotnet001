using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Net.Http.Headers;

namespace appsvc_function_ops_adduserwelcome_dotnet001
{
    class Auth
    {
        public GraphServiceClient graphAuth(ILogger log)
        {

            IConfiguration config = new ConfigurationBuilder()

           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
           .AddEnvironmentVariables()
           .Build();

            log.LogInformation("C# HTTP trigger function processed a request.");
           // var scopes = new string[] { "https://graph.microsoft.com/.default" };
           // var keyVaultUrl = config["keyVaultUrl"];
           // var keyname = "dgcx-dev-key-userssynch-"+rg_code;

            //SecretClientOptions options = new SecretClientOptions()
            //{
            //    Retry =
            //    {
            //        Delay= TimeSpan.FromSeconds(2),
            //        MaxDelay = TimeSpan.FromSeconds(16),
            //        MaxRetries = 5,
            //        Mode = RetryMode.Exponential
            //     }
            //};
            //var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(), options);

            //KeyVaultSecret secret = client.GetSecret(keyname);
            var scopes = new[] { "User.Read" };

            // Multi-tenant apps can use "common",
            // single-tenant apps must use the tenant ID from the Azure portal
            var tenantId = "28d8f6f0-3824-448a-9247-b88592acc8b7";

            // Value from app registration
            var clientId = "2a66bf9d-612b-41ad-9d4c-9631ede96a5c";

            // using Azure.Identity;
            var optionsAzIdentity = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var userName = "serviceAccount-AddWelcome@devgcx.ca";
            var password = "Boba7673";

            // https://docs.microsoft.com/dotnet/api/azure.identity.usernamepasswordcredential
            var userNamePasswordCredential = new UsernamePasswordCredential(
                userName, password, tenantId, clientId, optionsAzIdentity);

            var graphClient = new GraphServiceClient(userNamePasswordCredential, scopes);
            return graphClient;
        }

    }
}
