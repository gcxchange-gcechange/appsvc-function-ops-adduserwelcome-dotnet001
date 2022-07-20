using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace appsvc_function_ops_adduserwelcome_dotnet001
{
    public static class Globals
    {
        //Global class so other class can access variables
        static IConfiguration config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

        public static readonly string[] welcomeGroup = config["listWelcomeGroup"].Split(',');

    }
    public static class addusersAzureidentity
    {
        [FunctionName("addusersAzureidentity")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            Auth auth = new Auth();
            var graphAPIAuth = auth.graphAuth(log);

            IConfiguration config = new ConfigurationBuilder()

           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
           .AddEnvironmentVariables()
           .Build();

            //department group id of usersync
            var listgroupid = config["listgroupid"];

            string[] groupids = listgroupid.Split(',');

            foreach (var id in groupids)
            {
                var GetMembersList = getmember(graphAPIAuth, id, log).GetAwaiter().GetResult();

                foreach (var member in GetMembersList)
                {
                    log.LogInformation($"{member.DisplayName}-{member.CreatedDateTime}");
                    DateTime now = DateTime.Now;
                    if(member.CreatedDateTime > now.AddHours(-720))
                    {
                        var GetGroupMember = Usermember(graphAPIAuth, member.Id, log).GetAwaiter().GetResult();
                        if (GetGroupMember.Count() <= 0)
                        {
                            var AddUsertoGroup = addUserstowelcomeGroup(graphAPIAuth, member.Id, log).GetAwaiter().GetResult();
                        }
                    }
                }
            }
            return new OkObjectResult("OK");
        }

        public static async Task<List<User>> getmember(GraphServiceClient graphClient, string groupID, ILogger log)
        {
            List<User> users = new List<User>();
            try
            {
                var members = await graphClient.Groups[groupID].Members
                            .Request()
                            .Select("createdDateTime, displayName, id")
                            .Top(999)
                            .GetAsync();

                users.AddRange(members.CurrentPage.OfType<User>());
                // fetch next page
                while (members.NextPageRequest != null)
                {
                    members = await members.NextPageRequest.GetAsync();
                    users.AddRange(members.CurrentPage.OfType<User>());
                }

                return users;
            }
            catch (Exception ex)
            {
                
                log.LogInformation(ex.Message);
                return users;
            }
        }

        public static async Task<IDirectoryObjectCheckMemberGroupsCollectionPage> Usermember(GraphServiceClient graphClient, string userID, ILogger log)
        {
            IDirectoryObjectCheckMemberGroupsCollectionPage memberOf = new DirectoryObjectCheckMemberGroupsCollectionPage();
            try
            {
                memberOf = await graphClient.Users[userID]
                        .CheckMemberGroups(Globals.welcomeGroup)
                        .Request()
                        .PostAsync();

                return memberOf;
            }
            catch (Exception ex)
            {
                log.LogInformation("Error check user "+ex.Message);
                return memberOf;
            }
        }

        public static async Task<string> addUsers(GraphServiceClient graphClient, string userID, string groupid, ILogger log)
        {
            string response = "";
            try
            {
                var directoryObject = new DirectoryObject
                {
                    Id = userID
                };

                await graphClient.Groups[groupid].Members.References
                    .Request()
                    .AddAsync(directoryObject);

                response = $"User {userID} was add to {groupid}";
                log.LogInformation(response);
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                response = "Error";
            }
            return response;
        }

        public static async Task<string> addUserstowelcomeGroup(GraphServiceClient graphClient, string userID, ILogger log)
        {
            string response = "";
            foreach (var groupid in Globals.welcomeGroup)
            {
                try
                {
                    var queryOptions = new List<QueryOption>()
                    {
                        new QueryOption("$count", "true")
                    };

                    var members = await graphClient.Groups[groupid].Members
                        .Request(queryOptions)
                        .Header("ConsistencyLevel", "eventual")
                        .GetAsync();

                    if(members.Count <= 24990)
                    {
                        var AddUsertoGroup = addUsers(graphClient, userID, groupid, log).GetAwaiter().GetResult();
                        response = "user add";
                        break;
                    }
                }
                catch (Exception ex)
                {
                    log.LogInformation(ex.Message);
                    response = "Error";
                }
            }
            return response;
        }
    }
}
