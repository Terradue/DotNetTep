using System;
using System.Collections.Generic;
using System.Xml;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using Terradue.Cloud;
using Terradue.Github;
using Terradue.OpenNebula;
using Terradue.OpenNebula.WebService;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class OneUserServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetOneUsers request) {
            List<WebOneUser> result = new List<WebOneUser>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                int provId = (request.ProviderId != 0 ? request.ProviderId : context.GetConfigIntegerValue("One-default-provider"));
                OneCloudProvider oneCloud = (OneCloudProvider)CloudProvider.FromId(context, provId);
                USER_POOL pool = oneCloud.XmlRpc.UserGetPoolInfo();
                foreach(object u in pool.Items){
                    if(u is USER_POOLUSER){
                        USER_POOLUSER oneuser = u as USER_POOLUSER;
                        WebOneUser wu = new WebOneUser{ Id = oneuser.ID, Name = oneuser.NAME, Password = oneuser.PASSWORD, AuthDriver = oneuser.AUTH_DRIVER};
                        result.Add(wu);
                    }
                }
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(GetOneUser request) {
            WebOneUser result = null;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                int provId = (request.ProviderId != 0 ? request.ProviderId : context.GetConfigIntegerValue("One-default-provider"));
                OneCloudProvider oneCloud = (OneCloudProvider)CloudProvider.FromId(context, provId);
                USER oneuser = oneCloud.XmlRpc.UserGetInfo(request.Id);
                result = new WebOneUser{ Id = oneuser.ID, Name = oneuser.NAME, Password = oneuser.PASSWORD, AuthDriver = oneuser.AUTH_DRIVER};

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(GetOneCurrentUser request) {
            WebOneUser result = new WebOneUser();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                int provId = (request.ProviderId != 0 ? request.ProviderId : context.GetConfigIntegerValue("One-default-provider"));
                User user = User.FromId(context, context.UserId);
                string username = user.Email;
                try{
                    CloudUser usercloud = CloudUser.FromIdAndProvider(context, context.UserId, provId);
                    if(!String.IsNullOrEmpty(usercloud.CloudUsername)) username = usercloud.CloudUsername;
                }catch(Exception){}

                OneCloudProvider oneCloud = (OneCloudProvider)CloudProvider.FromId(context, provId);
                USER_POOL pool = oneCloud.XmlRpc.UserGetPoolInfo();
                foreach(object u in pool.Items){
                    if(u is USER_POOLUSER){
                        USER_POOLUSER oneuser = u as USER_POOLUSER;
                        if(oneuser.NAME == username){
                            result = new WebOneUser{ Id = oneuser.ID, Name = oneuser.NAME, Password = oneuser.PASSWORD, AuthDriver = oneuser.AUTH_DRIVER};
                            break;
                        }
                    }
                }

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(UpdateOneUser request) {
            bool result;
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                int provId = (request.ProviderId != 0 ? request.ProviderId : context.GetConfigIntegerValue("One-default-provider"));
                OneCloudProvider oneCloud = (OneCloudProvider)CloudProvider.FromId(context, provId);
                result = oneCloud.XmlRpc.UserUpdatePassword(Int32.Parse(request.Id), request.Password);
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Post(CreateOneUser request){
            WebOneUser result;
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                int provId = (request.ProviderId != 0 ? request.ProviderId : context.GetConfigIntegerValue("One-default-provider"));
                User user = User.FromId(context, context.UserId);
                string username = user.Email;
                try{
                    CloudUser usercloud = CloudUser.FromIdAndProvider(context, context.UserId, provId);
                    if(!String.IsNullOrEmpty(usercloud.CloudUsername)) username = usercloud.CloudUsername;
                }catch(Exception){}

                OneCloudProvider oneCloud = (OneCloudProvider)CloudProvider.FromId(context, provId);

                //create user
                int id = oneCloud.XmlRpc.UserAllocate(username, request.Password, (String.IsNullOrEmpty(request.AuthDriver) ? "x509" : request.AuthDriver));
                USER oneuser = oneCloud.XmlRpc.UserGetInfo(id);

                List<KeyValuePair<string, string>> templatePairs = new List<KeyValuePair<string, string>>();
                templatePairs.Add(new KeyValuePair<string, string>("USERNAME", username));
                templatePairs.Add(new KeyValuePair<string, string>("VM_USERNAME", user.Username));

                try{
                    GithubProfile github = GithubProfile.FromId(context, user.Id);
                    if(!String.IsNullOrEmpty(github.Name)) templatePairs.Add(new KeyValuePair<string, string>("GITHUB_USERNAME", github.Name));
                    if(!String.IsNullOrEmpty(github.Email)) templatePairs.Add(new KeyValuePair<string, string>("GITHUB_EMAIL", github.Email));
                    if(!String.IsNullOrEmpty(github.Token)) templatePairs.Add(new KeyValuePair<string, string>("GITHUB_TOKEN", github.Token));
                }catch(Exception){}

                //update user template
                string templateUser = CreateTemplate((XmlNode[])oneuser.TEMPLATE, templatePairs);
                if(!oneCloud.XmlRpc.UserUpdate(id, templateUser)) throw new Exception("Error during update of user");

                //add user to group GEP
                oneCloud.XmlRpc.UserUpdateGroup(id, context.GetConfigIntegerValue("One-GEP-grpID"));

                result = new WebOneUser{ Id = oneuser.ID, Name = oneuser.NAME, Password = oneuser.PASSWORD, AuthDriver = oneuser.AUTH_DRIVER};
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        private string CreateTemplate(XmlNode[] template, List<KeyValuePair<string, string>> pairs){
            List<KeyValuePair<string, string>> originalTemplate = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> resultTemplate = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < template.Length; i++) {
                originalTemplate.Add(new KeyValuePair<string, string>(template[i].Name, template[i].InnerText));
            }

            foreach (KeyValuePair<string, string> original in originalTemplate) {
                bool exists = false;
                foreach (KeyValuePair<string, string> pair in pairs) {
                    if (original.Key.Equals(pair.Key)) {
                        exists = true;
                        break;
                    }
                }
                if (!exists) pairs.Add(original);
            }

            string templateUser = "<TEMPLATE>";
            foreach(KeyValuePair<string, string> pair in pairs){
                templateUser += "<" + pair.Key + ">" + pair.Value + "</" + pair.Key + ">";
            }
            templateUser += "</TEMPLATE>";
            return templateUser;
        }

    }
}