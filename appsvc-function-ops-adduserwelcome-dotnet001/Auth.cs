using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;

namespace appsvc_function_ops_adduserwelcome_dotnet001
{
    //class Auth
    //{
    //    public GraphServiceClient graphAuth(ILogger log)
    //    {

    //        IConfiguration config = new ConfigurationBuilder()

    //       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    //       .AddEnvironmentVariables()
    //       .Build();

    //        log.LogInformation("C# HTTP trigger function processed a request.");
    //        var keyVaultUrl = config["keyVaultUrl"];
    //        var password = config["userpassword"];
    //        var username = config["user-email"];
    //        var tenantId = config["tenantid"];
    //        var clientId = config["clientId-delegated"];
    //        var scopes = new[] { "User.Read" };

    //        SecretClientOptions options = new SecretClientOptions()
    //        {
    //            Retry =
    //            {
    //                Delay= TimeSpan.FromSeconds(2),
    //                MaxDelay = TimeSpan.FromSeconds(16),
    //                MaxRetries = 5,
    //                Mode = RetryMode.Exponential
    //             }
    //        };
    //        var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(), options);

    //        KeyVaultSecret secret = client.GetSecret(password);

    //        // using Azure.Identity;
    //        var optionsAzIdentity = new TokenCredentialOptions
    //        {
    //            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
    //        };
    //        // https://docs.microsoft.com/dotnet/api/azure.identity.usernamepasswordcredential
    //        var userNamePasswordCredential = new UsernamePasswordCredential(
    //            username, secret.Value, tenantId, clientId, optionsAzIdentity);

    //        var graphClient = new GraphServiceClient(userNamePasswordCredential, scopes);
    //        return graphClient;
    //    }
    //}

    public class ROPCConfidentialTokenCredential : Azure.Core.TokenCredential
    {
        // Implementation of the Azure.Core.TokenCredential class
        string _clientId;
        string _clientSecret;
        string _password;
        string _tenantId;
        string _tokenEndpoint;
        string _username;
        ILogger _log;
        public ROPCConfidentialTokenCredential(ILogger log)
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();

            _username = config["user-email"];
            _tenantId = config["tenantid"];
            _clientId = config["clientId-delegated"];
            _tokenEndpoint = "https://login.microsoftonline.com/" + _tenantId + "/oauth2/v2.0/token";

            string keyVaultUrl = config["keyVaultUrl"];
            string secretName = config["secretName"];
            string secretNamePassword = config["secretNamePassword"];

            _log = log;

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
            var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(), options);

            KeyVaultSecret secret = client.GetSecret(secretName);
            _clientSecret = secret.Value;

            KeyVaultSecret password = client.GetSecret(secretNamePassword);
            _password = password.Value;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            HttpClient httpClient = new HttpClient();

            // Create the request body
            var Parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("scope", string.Join(" ", requestContext.Scopes)),
                new KeyValuePair<string, string>("username", _username),
                new KeyValuePair<string, string>("password", _password),
                new KeyValuePair<string, string>("grant_type", "password")
            };

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(Parameters)
            };
            var response = httpClient.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
            dynamic responseJson = JsonConvert.DeserializeObject(response);
            var expirationDate = DateTimeOffset.UtcNow.AddMinutes(60.0);
            return new AccessToken(responseJson.access_token.ToString(), expirationDate);
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            HttpClient httpClient = new HttpClient();

            // Create the request body
            var Parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("scope", string.Join(" ", requestContext.Scopes)),
                new KeyValuePair<string, string>("username", _username),
                new KeyValuePair<string, string>("password", _password),
                new KeyValuePair<string, string>("grant_type", "password")
            };

            //_log.LogInformation($"_clientId : {_clientId}");
            //_log.LogInformation($"_clientSecret : {_clientSecret}");
            //_log.LogInformation($"Scopes : {string.Join(" ", requestContext.Scopes)}");
            //_log.LogInformation($"_username : {_username}");
            //_log.LogInformation($"_password : {_password}");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(Parameters)
            };
            var response = httpClient.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
            dynamic responseJson = JsonConvert.DeserializeObject(response);
            var expirationDate = DateTimeOffset.UtcNow.AddMinutes(60.0);
            return new ValueTask<AccessToken>(new AccessToken(responseJson.access_token.ToString(), expirationDate));
        }
    }
}