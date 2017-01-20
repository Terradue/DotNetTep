using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using Terradue.OpenNebula;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
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

            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/{{Id}} GET Id='{0}'", request.Id));
                UserTep user = UserTep.FromId(context, request.Id);
                result = 
                    new WebUserTep(context, user);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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
        public object Get(UserGetCurrentRequestTep request) {
            WebUserTep result;
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/current GET"));
                UserTep user = UserTep.FromId(context, context.UserId);
                result = new WebUserTep(context, user, request.umsso);
                context.Close();
            } catch (Exception e) {
                //context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(UserGetCurrentSSORequestTep request) {
            WebUserTep result;
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/current/sso GET"));
                UserTep user = UserTep.FromId(context, context.UserId);
                user.FindTerradueCloudUsername();
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(UserGetSSORequestTep request) {
            WebUserTep result;
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/sso/{{Id}} GET Id='{0}'", request.Id));
                UserTep user = UserTep.FromId(context, request.Id);
                user.FindTerradueCloudUsername();
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(UserUpdateSSORequestTep request) {
            WebUserTep result;
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/sso PUT Id='{0}',T2Username='{1}'", request.Id, request.T2Username));
                UserTep user = UserTep.FromId(context, request.Id);
                user.TerradueCloudUsername = request.T2Username;
                user.StoreCloudUsername();
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Post(UserCurrentCreateSSORequestTep request) {
            WebUserTep result;
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/current/sso POST"));
                UserTep user = UserTep.FromId(context, context.UserId);
                user.CreateSSOAccount(request.Password);
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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

            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/{{id}}/public GET Id='{0}'", request.id));
                context.AccessLevel = EntityAccessLevel.Administrator;
                UserTep user = UserTep.GetPublicUser(context, request.id);
                result = new WebUserProfileTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/{{id}}/admin GET Id='{0}'", request.id));
                UserTep user = UserTep.FromId(context, request.id);
                result = new WebUserProfileTep(context, user);
                context.LogDebug(this,string.Format("Get public profile (admin view) for user '{0}'", user.Username));
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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

            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/public GET"));
                EntityList<UserTep> users = new EntityList<UserTep>(context);
                users.Load();
                foreach(UserTep u in users) result.Add(new WebUserProfileTep(context, u));
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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
            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            List<WebUser> result = new List<WebUser>();
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user GET"));

                EntityList<User> users = new EntityList<User>(context);
                users.Load();
                foreach(User u in users) result.Add(new WebUser(u));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebUserTep result;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user PUT Id='{0}'", request.Id));
				UserTep user = (request.Id == 0 ? null : UserTep.FromId(context, request.Id));

                user = request.ToEntity(context, user);
                user.Store();
                context.LogDebug(this,string.Format("User '{0}' has been updated", user.Username));
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(UserUpdateLevelRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebUserTep result;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/level PUT Id='{0}',Level='{1}'", request.Id, request.Level));
                UserTep user = (request.Id == 0 ? null : UserTep.FromId(context, request.Id));

                user.Level = request.Level;
                user.Store();
                context.LogDebug(this,string.Format("Level of user '{0}' has been updated to Level {1}", user.Username, request.Level));
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(UserUpdateAdminRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebUserTep result;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/admin PUT Id='{0}'", request.Id));
                UserTep user = (request.Id == 0 ? null : UserTep.FromId(context, request.Id));
                user.Level = request.Level;
                user.Store();
                context.LogDebug(this,string.Format("Level of user '{0}' has been updated to Level {1}", user.Username, request.Level));
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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
            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
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
                context.LogInfo(this,string.Format("/user POST Id='{0}'", user.Id));
                context.LogDebug(this,string.Format("User '{0}' has been created", user.Username));
                result = new WebUserTep(context , user);
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }
            return result;
        }

        public object Post (UserCreateApiKeyRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.UserView);
            WebUserTep result;
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/user/key POST Id='{0}'", context.UserId));

                UserTep user = UserTep.FromId (context, context.UserId);
                user.GenerateApiKey ();
                user.Store ();
                
                result = new WebUserTep (context, user);
                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }
            return result;
        }

        public object Delete (UserDeleteApiKeyRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.UserView);
            WebUserTep result;
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/user/key DELETE Id='{0}'", context.UserId));

                UserTep user = UserTep.FromId (context, context.UserId);
                user.ApiKey = null;
                user.Store ();

                result = new WebUserTep (context, user);
                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
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
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/{{Id}} DELETE Id='{0}'", request.Id));
                User user = User.FromId(context, request.Id);
                user.Delete();
                context.LogDebug(this,string.Format("User '{0}' has been deleted", user.Username));
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/current/logstatus GET"));
            } catch (Exception e) {
                return new WebResponseBool(false);
            }
            context.Close();
            return new WebResponseBool(true);
        }

        public object Get(UserGetUsageRequestTep request){
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/{{id}}/usage GET id='{0}'", request.Id));
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

                context.LogDebug(this,string.Format("Get User '{0}' usage", User.FromId(context, usu.UserId).Username));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get (UserSearchRequest request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            object result;
            context.Open ();
            context.LogInfo (this, string.Format ("/user/search GET"));

            EntityList<UserTep> users = new EntityList<UserTep> (context);
            users.Load ();

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            string format;
            if (Request.QueryString ["format"] == null)
                format = "atom";
            else
                format = Request.QueryString ["format"];

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest (httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query (users, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks (users, osr);
            //            OpenSearchFactory.ReplaceSelfLinks(wpsjobs, httpRequest.QueryString, osr.Result, EntrySelfLinkTemplate);

            context.Close ();
            return new HttpResult (osr.SerializeToString (), osr.ContentType);
        }

        public object Get (UserDescriptionRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/job/wps/description GET"));

                EntityList<UserTep> users = new EntityList<UserTep> (context);
                users.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = users.GetOpenSearchDescription ();

                context.Close ();

                return new HttpResult (osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }
        }

    }
}

