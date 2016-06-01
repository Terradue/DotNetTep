using System;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.Github;
using Terradue.Github.WebService;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class UserGithubServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Put(GetNewGithubToken request) {
            if (request.Code == null)
                throw new Exception("Code is empty");

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            WebGithubProfile result;

            try {
                context.Open();
                GithubProfile user = GithubProfile.FromId(context, context.UserId);
                //user.GetNewAuthorizationToken(request.Password, "write:public_key", "Terradue Sandboxes Application");
                user.GetNewAuthorizationToken(request.Code);
                log.InfoFormat("User {0} requested a new github token",user.Identifier);
                result = new WebGithubProfile(user);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }

            return result;
        }

        public object Post(AddGithubSSHKeyToCurrentUser request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            WebGithubProfile result;

            try {
                context.Open();

                GithubProfile user = GithubProfile.FromId(context, context.UserId);
                UserTep userTep = UserTep.FromId(context, context.UserId);
                userTep.LoadSSHPubKey();
                user.PublicSSHKey = userTep.SshPubKey;
                GithubClient githubClient = new GithubClient(context);
                if(!user.IsAuthorizationTokenValid()) throw new UnauthorizedAccessException("Invalid token");
                if(user.PublicSSHKey == null) throw new UnauthorizedAccessException("No available public ssh key");
                githubClient.AddSshKey("Terradue ssh key", user.PublicSSHKey, user.Token);
                log.InfoFormat("User {0} added Terradue ssh key to his github account",userTep.Username);
                result = new WebGithubProfile(user);
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }

            return result;
        }

        public object Get(GetGithubUser request){
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            WebGithubProfile result;

            try {
                context.Open();
                GithubProfile user = GithubProfile.FromId(context, context.UserId);
                UserTep userTep = UserTep.FromId(context, context.UserId);
                userTep.LoadSSHPubKey();
                user.PublicSSHKey = userTep.SshPubKey;
                result = new WebGithubProfile(user);
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }

            return result;
        }

        public object Put(UpdateGithubUser request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            WebGithubProfile result;

            try {
                context.Open();
                GithubProfile user = GithubProfile.FromId(context, request.Id);
                user = request.ToEntity(context, user);
                user.Store();
                user.Load(); //to get information from Github
                UserTep userTep = UserTep.FromId(context, context.UserId);
                userTep.LoadSSHPubKey();
                user.PublicSSHKey = userTep.SshPubKey;
                log.InfoFormat("User {0} has updated his github account",userTep.Username);
                result = new WebGithubProfile(user);
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }

            return result;
        }

    }
}

