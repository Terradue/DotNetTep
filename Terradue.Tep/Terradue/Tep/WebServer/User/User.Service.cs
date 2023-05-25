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

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/{{Id}} GET Id='{0}'", request.Id));
                UserTep user = UserTep.FromId(context, request.Id);
                result = new WebUserTep(context, user);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogInfo(this, string.Format("/user/current GET"));
                UserTep user = UserTep.FromId(context, context.UserId);
                try {
                    user.PrivateSanityCheck();//we do it here, because we do not want to do on each Load(), and we are sure users always pass by here
                }catch(Exception e){
                    context.LogError(this, e.Message, e);
                }                
                result = new WebUserTep(context, user, false);
                try{
                    var cookie = DBCookie.LoadDBCookie(context, System.Configuration.ConfigurationManager.AppSettings["PUBLISH_COOKIE_TOKEN"]);
                    TimeSpan span = cookie.Expire.Subtract(DateTime.UtcNow);
                    result.TokenExpire = span.TotalSeconds;
                    if(System.Configuration.ConfigurationManager.AppSettings["use_keycloak_exchange"] != null && System.Configuration.ConfigurationManager.AppSettings["use_keycloak_exchange"] == "true"){                    
                        var cookie2 = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-access"));
                        span = cookie2.Expire.Subtract(DateTime.UtcNow);
                        result.TokenExpire = Math.Min(result.TokenExpire, span.TotalSeconds);
                        if(result.TokenExpire < context.GetConfigIntegerValue("AccessTokenExpireMinutes")){                    
                            try{                                
                                var kfact = new KeycloakFactory(context);
                                kfact.GetExchangeToken(cookie2.Value);
                                cookie = DBCookie.LoadDBCookie(context, System.Configuration.ConfigurationManager.AppSettings["PUBLISH_COOKIE_TOKEN"]);
                            }catch(Exception e){
                                context.LogError(this, e.Message, e);    
                            }
                        }
                    }
                    result.Token = cookie.Value;                    

                }catch(Exception e){
                    context.LogError(this, e.Message, e);    
                }
                context.Close();
            } catch (Exception e) {
				context.LogError(this, e.Message, e);
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
                //user.FindTerradueCloudUsername();
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogInfo(this,string.Format("/user/sso/{{Id}} GET Id='{0}'", request.Identifier));
                UserTep user = UserTep.FromIdentifier(context, request.Identifier);
                //user.FindTerradueCloudUsername();
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogInfo(this,string.Format("/user/sso PUT Identifier='{0}',T2Username='{1}'", request.Identifier, request.T2Username));
                UserTep user = UserTep.FromIdentifier(context, request.Identifier);
                user.TerradueCloudUsername = request.T2Username;
                user.StoreCloudUsername();
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/{{id}}/public GET Id='{0}'", request.identifier));
                context.AccessLevel = EntityAccessLevel.Administrator;
                UserTep user = UserTep.GetPublicUser(context, request.identifier);
                result = new WebUserProfileTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/public GET"));
                EntityList<UserTep> users = new EntityList<UserTep>(context);
                users.Load();
                foreach(UserTep u in users) result.Add(new WebUserProfileTep(context, u));
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			List<WebUser> result = new List<WebUser>();
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user GET"));

				EntityList<User> users = new EntityList<User>(context);
                users.Load();
				foreach (User u in users) result.Add(new WebUser(u));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogInfo(this,string.Format("/user PUT Id='{0}'", request.Id > 0 ? request.Id + "" : request.Identifier));
				UserTep user = (request.Id == 0 ? (!string.IsNullOrEmpty(request.Identifier) ? UserTep.FromIdentifier(context, request.Identifier) : null) : UserTep.FromId(context, request.Id));
                if (context.UserId != user.Id && context.AccessLevel != EntityAccessLevel.Administrator) throw new Exception("Action not allowed");
                var level = user.Level;
                user = request.ToEntity(context, user);
                user.Level = level;//we can only change the level from the dedicated request (admin only)
                user.Store();
                context.LogDebug(this,string.Format("User '{0}' has been updated", user.Username));
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogInfo(this,string.Format("/user/level PUT Id='{0}',Level='{1}'", request.Id > 0 ? request.Id + "" : request.Identifier, request.Level));
                UserTep user = (request.Id == 0 ? (!string.IsNullOrEmpty(request.Identifier) ? UserTep.FromIdentifier(context, request.Identifier) : null) : UserTep.FromId(context, request.Id));

                user.Level = request.Level;
                user.Store();
                context.LogDebug(this,string.Format("Level of user '{0}' has been updated to Level {1}", user.Username, request.Level));
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(UserUpdateStatusRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebUserTep result;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/user/status PUT Id='{0}',Status='{1}'", request.Id > 0 ? request.Id + "" : request.Identifier, request.AccountStatus));
                UserTep user = (request.Id == 0 ? (!string.IsNullOrEmpty(request.Identifier) ? UserTep.FromIdentifier(context, request.Identifier) : null) : UserTep.FromId(context, request.Id));

                user.AccountStatus = request.AccountStatus;
                user.Store();
                context.LogDebug(this, string.Format("Status of user '{0}' has been updated to {1}", user.Username, request.AccountStatus));
                result = new WebUserTep(context, user);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
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
                context.LogError(this, e.Message, e);
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
                //user.GenerateApiKey ();
                //user.Store ();
                
                result = new WebUserTep (context, user);
                context.Close ();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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
				UserTep.UpdateUserSessionEndTime(context, context.UserId);
            } catch (Exception) {
                return new WebResponseBool(false);
            }
            context.Close();
            return new WebResponseBool(true);
        }

        public object Get(UserGetUsageRequestTep request){
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/{{id}}/usage GET id='{0}'", request.Id));
                var user = (request.Id != 0 ? User.FromId(context, request.Id) : User.FromUsername(context, request.Identifier));
                UserUsage usu = new UserUsage(context, user.Id);

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
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get (UserSearchRequest request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.UserView);
            context.Open ();
            context.LogInfo (this, string.Format ("/user/search GET"));

            EntityList<UserTep> users = new EntityList<UserTep> (context);
            users.AddSort("Identifier", SortDirection.Ascending);

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            string format;
            if (Request.QueryString ["format"] == null)
                format = "atom";
            else
                format = Request.QueryString ["format"];

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest.QueryString, httpRequest.Headers, ose);
            IOpenSearchResultCollection osr = ose.Query (users, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks (users, osr);

            context.Close ();
            return new HttpResult (osr.SerializeToString (), osr.ContentType);
        }

        public object Get (UserDescriptionRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/user/description GET"));

                EntityList<UserTep> users = new EntityList<UserTep> (context);
                users.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = users.GetOpenSearchDescription ();

                context.Close ();

                return new HttpResult (osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }
        }

        public object Get(UserCsvListRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            var csv = new System.Text.StringBuilder();
            var filename = DateTime.UtcNow.ToString("yy-MM-dd");
            try {
                context.Open();
                context.LogInfo(this, string.Format("/user/csv GET"));
                filename = context.GetConfigValue("SiteNameShort") + "-" + filename;

                string sql = string.Format("SELECT usr.id,usr.username,usr.email,usr.firstname,usr.lastname,usr.level,usr.affiliation,usr.country,(SELECT log_time FROM usrsession WHERE id_usr=usr.id ORDER BY log_time ASC LIMIT 1) AS registration_date FROM usr;");
                csv.Append("Id,Username,Email,FisrtName,LastName,Level,Affiliation,Country,Registration date" + Environment.NewLine);
                System.Data.IDbConnection dbConnection = context.GetDbConnection();
                System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
                while (reader.Read()) {
                    csv.Append(String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}{9}", 
                                             reader.GetValue(0) != DBNull.Value ? reader.GetString(0) : "",
                                             reader.GetValue(1) != DBNull.Value ? reader.GetString(1) : "",
                                             reader.GetValue(2) != DBNull.Value ? reader.GetString(2) : "",
                                             reader.GetValue(3) != DBNull.Value ? reader.GetString(3) : "",
                                             reader.GetValue(4) != DBNull.Value ? reader.GetString(4) : "",
                                             reader.GetValue(5) != DBNull.Value ? reader.GetString(5) : "",
                                             reader.GetValue(6) != DBNull.Value ? reader.GetString(6) : "",
                                             reader.GetValue(7) != DBNull.Value ? reader.GetString(7) : "",
                                             reader.GetValue(8) != DBNull.Value ? reader.GetString(8) : "",
                                             Environment.NewLine));
                }
                context.CloseQueryResult(reader, dbConnection);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}.csv", filename));
            return csv.ToString();
        }

        public object Get(UserHasT2NotebooksRequestTep request) {
            bool result = false;

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/user/{{usrId}}/notebooks GET usrId='{0}'", request.UsrId));

                UserTep user = UserTep.FromIdentifier(context, request.UsrId);
                if (context.AccessLevel == EntityAccessLevel.Administrator || context.Username == request.UsrId) result = user.HasT2NotebooksAccount();
                
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(UpdateProfileFromRemoteTep request) {
            WebUserTep result = null;
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            context.Open();
            var usr = UserTep.FromIdentifier(context, request.Identifier);
            try
            {
                usr.LoadProfileFromRemote();
                result = new WebUserTep(context, usr);
            }catch(Exception e){
                context.LogError(this, e.Message + " - " + e.StackTrace);
            }
            context.Close();
            return result;   
        }

        public object Put(UpdateBulkUsersLevelTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/users/level PUT Ids='{0}',level='{1}'", string.Join(",", request.Identifiers), request.Level));

                string ids = "";
                foreach(var id in request.Identifiers) ids += string.Format("'{0}',", id);
                ids = ids.TrimEnd(',');
                string sql = string.Format("UPDATE usr SET level='{0}' WHERE username IN ({1});", request.Level, ids);
                context.Execute(sql);

                try{
                    var portalname = string.Format("{0} Portal", context.GetConfigValue("SiteNameShort"));
                    var subject = context.GetConfigValue("EmailBulkActionSubject");
                    subject = subject.Replace("$(SITENAME)",portalname);
                    var body = context.GetConfigValue("EmailBulkActionBody");
                    body = body.Replace("$(ADMIN)", context.Username);
                    body = body.Replace("$(ACTION)", "User level update");
                    body = body.Replace("$(IDENTIFIERS)", string.Join(",", request.Identifiers));
                    context.SendMail(context.GetConfigValue("SmtpUsername"), context.GetConfigValue("SmtpUsername"), subject, body);
                } catch(Exception e){
                    context.LogError(this,e.Message + " - " + e.StackTrace);
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return true;
        }

        public object Put(UpdateBulkUsersProfileFromRemoteTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/users/profile PUT Ids='{0}'", string.Join(",", request.Identifiers)));

                foreach(var identifier in request.Identifiers){                    
                    try
                    {
                        var usr = UserTep.FromIdentifier(context, identifier);
                        usr.LoadProfileFromRemote();
                    }catch(Exception e){
                        context.LogError(this, e.Message + " - " + e.StackTrace);
                    }
                }

                try{
                    var portalname = string.Format("{0} Portal", context.GetConfigValue("SiteNameShort"));
                    var subject = context.GetConfigValue("EmailBulkActionSubject");
                    subject = subject.Replace("$(SITENAME)",portalname);
                    var body = context.GetConfigValue("EmailBulkActionBody");
                    body = body.Replace("$(ADMIN)", context.Username);
                    body = body.Replace("$(ACTION)", "User remote profile load");
                    body = body.Replace("$(IDENTIFIERS)", string.Join(",", request.Identifiers));
                    context.SendMail(context.GetConfigValue("SmtpUsername"), context.GetConfigValue("SmtpUsername"), subject, body);
                } catch(Exception e){
                    context.LogError(this,e.Message + " - " + e.StackTrace);
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return true;
        }

        public object Delete(UserDeleteRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            context.Open();
            var usr = UserTep.FromIdentifier(context, request.UsrId);            
            try{
                var portalname = string.Format("{0} Portal", context.GetConfigValue("SiteNameShort"));
                var subject = context.GetConfigValue("EmailUserDeleteSubject");
                subject = subject.Replace("$(SITENAME)",portalname);
                var body = context.GetConfigValue("EmailUserDeleteBody");
                body = body.Replace("$(USERNAME)", usr.Username);
                body = body.Replace("$(ADMIN)", context.Username);                
                context.SendMail(context.GetConfigValue("SmtpUsername"), context.GetConfigValue("SmtpUsername"), subject, body);
            } catch(Exception e){
                context.LogError(this,e.Message + " - " + e.StackTrace);
            }
            usr.Delete();
            context.Close();
            return true;   
        }

    }
}

