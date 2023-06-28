using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web;
using Terradue.Portal;

namespace Terradue.Tep
{

    /// <summary>
    /// Authentication open identifier.
    /// /// </summary>
    public class TepOauthAuthenticationType : AuthenticationType {

        static System.Collections.Specialized.NameValueCollection AppSettings = System.Configuration.ConfigurationManager.AppSettings;		

        public TepOauthClient Client;

        /// <summary>
        /// Indicates whether the authentication type depends on external identity providers.
        /// </summary>
        /// <value><c>true</c> if uses external identity provider; otherwise, <c>false</c>.</value>
        public override bool UsesExternalIdentityProvider {
            get {
                return true;
            }
        }

        /// <summary>
        /// In a derived class, checks whether an session corresponding to the current web server session exists on the
        /// external identity provider.
        /// </summary>
        /// <returns><c>true</c> if this instance is external session active the specified context request; otherwise, <c>false</c>.</returns>
        /// <param name="context">Context.</param>
        /// <param name="request">Request.</param>
        public override bool IsExternalSessionActive(IfyWebContext context, HttpRequest request) {
            return true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.OAuth.EoschubAuthenticationType"/> class.
        /// </summary>
        public TepOauthAuthenticationType(IfyContext context) : base(context) {
            Client = new TepOauthClient(context);
        }

        /// <summary>
        /// Sets the Openid Client.
        /// </summary>
        /// <param name="c">Client.</param>
        public void SetCLient(TepOauthClient c) {
            this.Client = c;
        }

        /// <summary>
        /// Check if refresh is needed
        /// </summary>
        /// <returns></returns>
        public void CheckRefresh() {            
            var accessCookie = Client.LoadTokenAccess();                     
            TimeSpan span = accessCookie.Expire.Subtract(DateTime.UtcNow);            
            if (span.TotalMinutes < context.GetConfigIntegerValue("AccessTokenExpireMinutes")) {
                context.LogDebug(this, string.Format("CheckRefresh : {0} minutes remaining", span.TotalMinutes));
                var refreshCookie = Client.LoadTokenRefresh();
                var refreshToken = refreshCookie.Value;
                context.LogDebug(this, string.Format("CheckRefresh : load refresh token"));
                Client.RefreshToken(refreshToken, refreshCookie.Username);
            }

            //always update session cookies in case of EULA update
            var idTokenCookie = Client.LoadTokenId();
            CookiesFactory.StoreSessionCookies(context, idTokenCookie.Value, idTokenCookie.Expire);
        }
        
        /// <summary>
        /// Check if refresh is needed
        /// </summary>
        /// <returns></returns>
        public void CheckRefreshForUser(string username){
            var accessCookie = DBCookie.FromUsernameAndIdentifier(context, username, TepOauthClient.COOKIE_TOKEN_ACCESS);
            TimeSpan span = accessCookie.Expire.Subtract(DateTime.UtcNow);
            if (span.TotalMinutes < context.GetConfigIntegerValue("AccessTokenExpireMinutes")) {
                context.LogDebug(this, string.Format("CheckRefreshForUser : {0} minutes remaining", span.TotalMinutes));
                var refreshCookie = DBCookie.FromUsernameAndIdentifier(context, username, TepOauthClient.COOKIE_TOKEN_REFRESH);
                var refreshToken = refreshCookie.Value;
                Client.RefreshToken(refreshToken, username);
            }
        }

        public void ForceRefreshIdTokenForUser(string username){
            var refreshCookie = DBCookie.FromUsernameAndIdentifier(context, username, TepOauthClient.COOKIE_TOKEN_REFRESH);
            var refreshToken = refreshCookie.Value;
            var tokenResponse = Client.RefreshToken(refreshToken, username);

            var session = refreshCookie.Session;

            context.LogDebug(this, "StoreTokenAccess - " + session + " - " + username);
			DBCookie.DeleteDBCookie(context, TepOauthClient.COOKIE_TOKEN_ACCESS);
            DBCookie.StoreDBCookie(context, session, TepOauthClient.COOKIE_TOKEN_ACCESS, tokenResponse.access_token, username, DateTime.UtcNow.AddSeconds(tokenResponse.expires_in));            

			context.LogDebug(this, "StoreTokenRefresh - " + session + " - " + username);
            DBCookie.DeleteDBCookie(context, TepOauthClient.COOKIE_TOKEN_REFRESH);
			DBCookie.StoreDBCookie(context, session, TepOauthClient.COOKIE_TOKEN_REFRESH, tokenResponse.refresh_token, username, DateTime.UtcNow.AddSeconds(tokenResponse.expires_in));

			if(!string.IsNullOrEmpty(tokenResponse.id_token)) {
                context.LogDebug(this, "StoreTokenId - " + session + " - " + username);
                DBCookie.DeleteDBCookie(context, TepOauthClient.COOKIE_TOKEN_ID);
			    DBCookie.StoreDBCookie(context, session, TepOauthClient.COOKIE_TOKEN_ID, tokenResponse.id_token, username, DateTime.UtcNow.AddSeconds(tokenResponse.expires_in));            
            }
        }

        /// <summary>
        /// Gets the user profile.
        /// </summary>
        /// <returns>The user profile.</returns>
        /// <param name="context">Context.</param>
        /// <param name="request">Request.</param>
        public override User GetUserProfile(IfyWebContext context, HttpRequest request = null, bool strict = false) {
            User usr = null;
            AuthenticationType authType = IfyWebContext.GetAuthenticationType(typeof(TepOauthAuthenticationType));

            var refreshCookie = Client.LoadTokenRefresh();
            var accessCookie = Client.LoadTokenAccess();
            var refreshToken = refreshCookie.Value;
            var accessToken = accessCookie.Value;

            TimeSpan span = accessCookie.Expire.Subtract(DateTime.UtcNow);

            var refresh = (!string.IsNullOrEmpty(refreshToken) && (string.IsNullOrEmpty(accessToken) || span.TotalMinutes < context.GetConfigIntegerValue("AccessTokenExpireMinutes")));

            if (refresh) {
                context.LogDebug(this, "Refresh token");
                // refresh the token
                try {
                    Client.RefreshToken(refreshToken, refreshCookie.Username);
                    refreshToken = Client.LoadTokenRefresh().Value;
                    accessToken = Client.LoadTokenAccess().Value;
                } catch (Exception e) {
                    context.LogError(this, e.Message);
                    return null;
                }
            }             

            if (!string.IsNullOrEmpty(accessToken)) {
                context.LogDebug(this, "Get user info");
                TepOauthUserInfoResponse usrInfo;                
                try {
                    usrInfo = Client.GetUserInfo<TepOauthUserInfoResponse>(accessToken);
                } catch (Exception e) {
                    context.LogError(this, e.Message);
                    return null;
                }
                if (usrInfo == null) return null;

                var username_field = context.GetConfigValue("sso_username_field");
                if(string.IsNullOrEmpty(username_field)) username_field = "sub";
                string username;

                switch(username_field){
                    case "email":
                        username = usrInfo.email;
                        break;
                    case "screenname":
                        username = usrInfo.screenname;
                        break;
                    case "name":
                        username = usrInfo.name;
                        break;
                    case "preferred_username":
                        username = usrInfo.preferred_username;
                        break;
                    case "given_name":
                        username = usrInfo.given_name;
                        break;
                    case "family_name":
                        username = usrInfo.family_name;
                        break;
                    case "userId":
                        username = usrInfo.userId;
                        break;
                    case "sub":
                        username = usrInfo.sub;
                        break;
                    default:
                        username = usrInfo.sub;
                        break;
                }


                context.LogDebug(this, "Get user info  - " + username_field + " = " + usrInfo.sub);

                context.AccessLevel = EntityAccessLevel.Administrator;

                //first try to get user from username
                try {
                    usr = User.FromUsername(context, username);
                    usr.AccountStatus = AccountStatusType.Enabled;
                } catch (Exception) {
                    try {
                        usr = User.GetOrCreate(context, username, authType);                        
                        usr.AccountStatus = AccountStatusType.Enabled;
                    } catch (Exception e2) {
                        Client.RevokeSessionCookies();
                        throw e2;
                    }
                }
                bool isnew = (usr.Id == 0);

                //update user infos
                if (!string.IsNullOrEmpty(usrInfo.given_name)) usr.FirstName = usrInfo.given_name;
                if (!string.IsNullOrEmpty(usrInfo.email)) usr.Email = usrInfo.email;
                if (!string.IsNullOrEmpty(usrInfo.family_name)) usr.LastName = usrInfo.family_name;                
                usr.Store();

                if (isnew) usr.LinkToAuthenticationProvider(authType, usrInfo.sub);

                //roles
                if (usrInfo.roles != null){

                    //for now, if user has a role, he can process
                    if (usr.Level < UserLevel.Manager) {
                        usr.Level = UserLevel.Manager;
                        usr.Store();
                    }

                    UserTep usrtep = UserTep.FromId(context, usr.Id);
                }

                return usr;
            }

            return null;
        }

        public override void EndExternalSession(IfyWebContext context, HttpRequest request, HttpResponse response) {            
            Client.RevokeSessionCookies();
        }
    }

    [DataContract]
    public class OauthUserInfoResponseBase
    {
        [DataMember]
        public string sub { get; set; }
        [DataMember]
        public string screenname { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string preferred_username { get; set; }
        [DataMember]
        public string given_name { get; set; }
        [DataMember]
        public string family_name { get; set; }
        [DataMember]
        public string email { get; set; }
        [DataMember]
        public string userId { get; set; }        
    }

    public class TepOauthUserInfoResponse : OauthUserInfoResponseBase
    {
        [DataMember]
        public List<string> roles { get; set; }
    }

}


