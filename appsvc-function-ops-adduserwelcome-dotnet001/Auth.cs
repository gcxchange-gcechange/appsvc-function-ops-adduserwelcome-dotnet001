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
            var keyVaultUrl = config["keyVaultUrl"];
            var password = config["userpassword"];
            var username = config["user-email"];
            var tenantId = config["tenantid"];
            var clientId = config["clientId-delegated"];
            log.LogInformation("test1");
            var scopes = new[] { "User.Read" };

            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                 }
            };
            log.LogInformation("test3"+ password + keyVaultUrl);
            var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(), options);

            KeyVaultSecret secret = client.GetSecret(password);

            // using Azure.Identity;
            var optionsAzIdentity = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            log.LogInformation("test4");
            // https://docs.microsoft.com/dotnet/api/azure.identity.usernamepasswordcredential
            var userNamePasswordCredential = new UsernamePasswordCredential(
                username, secret.Value, tenantId, clientId, optionsAzIdentity);

            log.LogInformation("test5");

            var graphClient = new GraphServiceClient(userNamePasswordCredential, scopes);
            log.LogInformation("test2");
            return graphClient;
        }
    }
}
