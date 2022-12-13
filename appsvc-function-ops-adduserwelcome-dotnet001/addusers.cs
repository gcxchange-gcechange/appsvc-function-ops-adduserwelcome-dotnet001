using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Diagnostics.Metrics;

namespace appsvc_function_ops_adduserwelcome_dotnet001
{
    public static class Globals
    {
        //Global class so other class can access variables
        static IConfiguration config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

        public static readonly string assignedGroup = config["assignedGroupId"];
        public static readonly string[] welcomeGroup = config["listWelcomeGroup"].Split(',');
    }
    public static class addusersAzureidentity
    {
        [FunctionName("addusersAzureidentity")]
        public static async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer, ExecutionContext context, ILogger log)
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();

            // department group id of usersync
            var listgroupid = config["listgroupid"];
            string[] groupids = listgroupid.Split(',');

            ROPCConfidentialTokenCredential auth = new ROPCConfidentialTokenCredential(log);
            var graphAPIAuth = new GraphServiceClient(auth);

            foreach (var id in groupids)
            {
                log.LogInformation($"id: {id}");

                var GetMembersList = getmember(graphAPIAuth, id, log).GetAwaiter().GetResult();

                foreach (var member in GetMembersList)
                {
                    log.LogInformation($"{member.Id}-{member.DisplayName}-{member.CreatedDateTime}");

                    // add user to Assigned Group if not already a member
                    var GetAssignedGroupMember = Usermember(graphAPIAuth, new string[] { Globals.assignedGroup }, member.Id, log).GetAwaiter().GetResult();
                    if (GetAssignedGroupMember.Count() <= 0)
                    {
                        addUsersToAssignedGroup(graphAPIAuth, member.Id, log).GetAwaiter().GetResult();
                    }

                    // add user to Welcome Group if not already a member and creation date less than 14 days
                    DateTime now = DateTime.Now;
                    if (member.CreatedDateTime > now.AddHours(-720))
                    {
                        var GetWelcomeGroupMember = Usermember(graphAPIAuth, Globals.welcomeGroup, member.Id, log).GetAwaiter().GetResult();
                        if (GetWelcomeGroupMember.Count() <= 0)
                        {
                            addUserstowelcomeGroup(graphAPIAuth, member.Id, log).GetAwaiter().GetResult();
                        }
                    }
                }
            }
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

        public static async Task<IDirectoryObjectCheckMemberGroupsCollectionPage> Usermember(GraphServiceClient graphClient, string[] groupID, string userID, ILogger log)
        {
            IDirectoryObjectCheckMemberGroupsCollectionPage memberOf = new DirectoryObjectCheckMemberGroupsCollectionPage();
            try
            {
                memberOf = await graphClient.Users[userID]
                        .CheckMemberGroups(groupID)
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

                    var group = await graphClient.Groups[groupid].Request().GetAsync();
                    var members = await graphClient.Groups[groupid].Members.Request(queryOptions).Header("ConsistencyLevel", "eventual").GetAsync();

                    if(members.Count <= 24990)
                    {
                        var AddUsertoGroup = addUsers(graphClient, userID, groupid, log).GetAwaiter().GetResult();
                        response = "user added to welcome group";
                        break;
                    }
                }
                catch (Exception e)
                {
                    log.LogInformation($"Message: {e.Message}");
                    if (e.InnerException is not null)
                        log.LogInformation($"InnerException: {e.InnerException.Message}");
                }
            }
            return response;
        }

        public static async Task<string> addUsersToAssignedGroup(GraphServiceClient graphClient, string userID, ILogger log)
        {
            string response = "";

            var groupid = Globals.assignedGroup;

            try
            {
                var group = await graphClient.Groups[groupid].Request().GetAsync();
                var members = await graphClient.Groups[groupid].Members.Request().Header("ConsistencyLevel", "eventual").GetAsync();

                var AddUsertoGroup = addUsers(graphClient, userID, groupid, log).GetAwaiter().GetResult();
                response = "user added to assigned group";
            }
            catch (Exception e)
            {
                log.LogInformation($"Message: {e.Message}");
                if (e.InnerException is not null)
                    log.LogInformation($"InnerException: {e.InnerException.Message}");
            }

            return response;
        }
    }
}