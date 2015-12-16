using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using Terradue.OpenNebula;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class UserServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(UserGetRequestTep request) {
            WebUserTep result;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                UserTep user = UserTep.FromId(context, request.Id);
                result = 
                    new WebUserTep(context, user);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Get the current user.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <returns>the current user</returns>
        public object Get(GetCurrentUser request) {
            WebUserTep result;
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                UserTep user = UserTep.FromId(context, context.UserId);
                log.Info(String.Format("Get current user '{0}'", user.Username));
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(UserGetPublicProfileRequestTep request) {
            WebUserProfileTep result;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.RestrictedMode = false;
                UserTep user = UserTep.GetPublicUser(context, request.username);
                result = new WebUserProfileTep(context, user);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(UserGetProfileAdminRequestTep request) {
            WebUserTep result;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                UserTep user = (UserTep)UserTep.FromUsername(context, request.username);
                result = new WebUserProfileTep(context, user);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(UserGetPublicProfilesRequestTep request) {
            List<WebUserProfileTep> result = new List<WebUserProfileTep>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                EntityList<UserTep> users = new EntityList<UserTep>(context);
                users.Load();
                foreach(UserTep u in users) result.Add(new WebUserProfileTep(context, u));

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Get the list of all users.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <returns>the users</returns>
        public object Get(GetUsers request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            List<WebUser> result = new List<WebUser>();
            try {
                context.Open();

                EntityList<User> users = new EntityList<User>(context);
                users.Load();
                foreach(User u in users) result.Add(new WebUser(u));

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Update the specified user.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <returns>the user</returns>
        public object Put(UserUpdateRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebUserTep result;
            try {
                context.Open();
				UserTep user = (request.Id == 0 ? null : UserTep.FromId(context, request.Id));

                user = request.ToEntity(context, user);
                user.Store();
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(UserUpdateAdminRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebUserTep result;
            try {
                context.Open();
                UserTep user = (request.Id == 0 ? null : UserTep.FromId(context, request.Id));
                user.Level = request.Level;
                user.Store();
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(UserCreateRequestTep request)
        {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebUserTep result;
            try{
                context.Open();
				UserTep user = (request.Id == 0 ? null : UserTep.FromId(context, request.Id));
				user = request.ToEntity(context, user);
                if(request.Id != 0 && context.UserLevel == UserLevel.Administrator){
                    user.AccountStatus = AccountStatusType.Enabled;
                }
                else{
                    user.AccountStatus = AccountStatusType.PendingActivation;
                }

                user.IsNormalAccount = true;
                user.Level = UserLevel.User;

                user.Store();

                result = new WebUserTep(context , user);
                context.Close ();
            }catch(Exception e) {
                context.Close ();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Delete the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Delete(DeleteUser request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                User user = User.FromId(context, request.Id);
                user.Delete();

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(UserCurrentIsLoggedRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
            } catch (Exception e) {
                return new WebResponseBool(false);
            }
            context.Close();
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(UserGetGroupsRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();

                UserTep user = UserTep.FromId(context, request.UsrId);
                List<int> ids = user.GetGroups();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    result.Add(new WebGroup(grp));
                }

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(UserGetUsageRequestTep request){
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                UserUsage usu = new UserUsage(context, request.Id);

                //global usage
                result.Add(new KeyValuePair<string, string>("overall","" + usu.GetScore()));

                //data package usage
                List<Type> types = new List<Type>();
                types.Add(typeof(DataPackage));
                types.Add(typeof(RemoteResource));
                types.Add(typeof(RemoteResourceSet));
                result.Add(new KeyValuePair<string, string>("data/package","" + usu.GetScore(types)));

                //wps job usage
                types = new List<Type>();
                types.Add(typeof(WpsJob));
                result.Add(new KeyValuePair<string, string>("job/wps","" + usu.GetScore(types)));

                //wps service usage
                types = new List<Type>();
                types.Add(typeof(Terradue.Portal.Service));
                types.Add(typeof(WpsProcessOffering));
                result.Add(new KeyValuePair<string, string>("service/wps","" + usu.GetScore(types)));


                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

    }
}

