using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web;
using Terradue.Portal;

namespace Terradue.Tep.Controller.Auth
{

    /// <summary>
    /// /// Authentication open identifier.
    /// </summary>
    public class KeycloakAuthenticationType : AuthenticationType {

        private KeycloakOauthClient client;

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
        public KeycloakAuthenticationType(IfyContext context) : base(context) {
            client = new KeycloakOauthClient(context);
        }

        /// <summary>
        /// Sets the Openid Client.
        /// </summary>
        /// <param name="c">Client.</param>
        public void SetCLient(KeycloakOauthClient c) {
            this.client = c;
        }

        /// <summary>
        /// Check if refresh is needed
        /// </summary>
        /// <returns></returns>
        public void CheckRefresh() {
            var accessCookie = client.LoadTokenAccess();
            TimeSpan span = accessCookie.Expire.Subtract(DateTime.UtcNow);
            if (span.TotalMinutes < context.GetConfigIntegerValue("AccessTokenExpireMinutes")) {
                context.LogDebug(this, string.Format("CheckRefresh : {0} minutes remaining", span.TotalMinutes));
                var refreshCookie = client.LoadTokenRefresh();
                var refreshToken = refreshCookie.Value;
                client.RefreshToken(refreshToken, refreshCookie.Username);
            }

            //always update session cookies in case of EULA update
            var idTokenCookie = client.LoadTokenId();
            client.StoreSessionCookies(idTokenCookie.Value, idTokenCookie.Expire);
        }
        
        /// <summary>
        /// Check if refresh is needed
        /// </summary>
        /// <returns></returns>
        public void CheckRefreshForUser(string username){
            var accessCookie = DBCookie.FromUsernameAndIdentifier(context, username, KeycloakOauthClient.COOKIE_TOKEN_ACCESS);
            TimeSpan span = accessCookie.Expire.Subtract(DateTime.UtcNow);
            if (span.TotalMinutes < context.GetConfigIntegerValue("AccessTokenExpireMinutes")) {
                context.LogDebug(this, string.Format("CheckRefreshForUser : {0} minutes remaining", span.TotalMinutes));
                var refreshCookie = DBCookie.FromUsernameAndIdentifier(context, username, KeycloakOauthClient.COOKIE_TOKEN_REFRESH);
                var refreshToken = refreshCookie.Value;
                client.RefreshToken(refreshToken, username);
            }
        }

        public void ForceRefreshIdTokenForUser(string username){
            var refreshCookie = DBCookie.FromUsernameAndIdentifier(context, username, KeycloakOauthClient.COOKIE_TOKEN_REFRESH);
            var refreshToken = refreshCookie.Value;
            var tokenResponse = client.RefreshToken(refreshToken, username);

            var session = refreshCookie.Session;

            DBCookie.DeleteDBCookiesFromUsername(context, username);

            context.LogDebug(this, "StoreTokenAccess - " + session + " - " + username);
			DBCookie.StoreDBCookie(context, session, KeycloakOauthClient.COOKIE_TOKEN_ACCESS, tokenResponse.access_token, username, DateTime.UtcNow.AddSeconds(tokenResponse.expires_in));            

			context.LogDebug(this, "StoreTokenRefresh - " + session + " - " + username);
			DBCookie.StoreDBCookie(context, session, KeycloakOauthClient.COOKIE_TOKEN_REFRESH, tokenResponse.refresh_token, username, DateTime.UtcNow.AddSeconds(tokenResponse.expires_in));

			if(!string.IsNullOrEmpty(tokenResponse.id_token)) {
                context.LogDebug(this, "StoreTokenId - " + session + " - " + username);
			    DBCookie.StoreDBCookie(context, session, KeycloakOauthClient.COOKIE_TOKEN_ID, tokenResponse.id_token, username, DateTime.UtcNow.AddSeconds(tokenResponse.expires_in));            
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
            AuthenticationType authType = IfyWebContext.GetAuthenticationType(typeof(KeycloakAuthenticationType));

            var refreshCookie = client.LoadTokenRefresh();
            var accessCookie = client.LoadTokenAccess();
            var refreshToken = refreshCookie.Value;
            var accessToken = accessCookie.Value;

            TimeSpan span = accessCookie.Expire.Subtract(DateTime.UtcNow);

            var refresh = (!string.IsNullOrEmpty(refreshToken) && (string.IsNullOrEmpty(accessToken) || span.TotalMinutes < context.GetConfigIntegerValue("AccessTokenExpireMinutes")));

            if (refresh) {
                context.LogDebug(this, "Refresh token");
                // refresh the token
                try {
                    client.RefreshToken(refreshToken, refreshCookie.Username);
                    refreshToken = client.LoadTokenRefresh().Value;
                    accessToken = client.LoadTokenAccess().Value;
                } catch (Exception e) {
                    context.LogError(this, e.Message);
                    return null;
                }
            }             

            if (!string.IsNullOrEmpty(accessToken)) {
                context.LogDebug(this, "Get user info");
                KeycloakOauthUserInfoResponse usrInfo;
                try {
                    usrInfo = client.GetUserInfo<KeycloakOauthUserInfoResponse>(accessToken);
                } catch (Exception e) {
                    context.LogError(this, e.Message);
                    return null;
                }
                if (usrInfo == null) return null;

                context.LogDebug(this, "Get user info  - sub = " + usrInfo.sub);

                context.AccessLevel = EntityAccessLevel.Administrator;

                //first try to get user from username, as it may have been inserted from Activation notification
                 try {
                    usr = User.FromUsername(context, usrInfo.preferred_username);
                    usr.AccountStatus = AccountStatusType.Enabled;
                } catch (Exception) {
                    try {
                        usr = User.FromUsername(context, usrInfo.sub);
                        usr.AccountStatus = AccountStatusType.Enabled;
                    } catch (Exception) {
                        try {
                            usr = User.GetOrCreate(context, usrInfo.sub, authType);
                            if (usr.Id ==0) usr.Username = usrInfo.preferred_username;
                            usr.AccountStatus = AccountStatusType.Enabled;
                        } catch (Exception e2) {
                            client.RevokeSessionCookies();
                            throw e2;
                        }
                    }
                }
                bool isnew = (usr.Id == 0);

                //update user infos
                if (!string.IsNullOrEmpty(usrInfo.screenname)) usr.FirstName = usrInfo.screenname;
                if (!string.IsNullOrEmpty(usrInfo.email) && string.IsNullOrEmpty(usr.Email)) usr.Email = usrInfo.email;
                if (isnew && !string.IsNullOrEmpty(usrInfo.screenname)) usr.Username = usrInfo.screenname;
                usr.Store();

                if (isnew) usr.LinkToAuthenticationProvider(authType, usrInfo.sub);               

                //roles
                if (usrInfo.roles != null){

                    //for now, if user has a role, he can process
                    if (usr.Level < UserLevel.Manager) {
                        usr.Level = UserLevel.Manager;
                        usr.Store();
                    }             
                }

                return usr;
            }

            return null;
        }

        public override void EndExternalSession(IfyWebContext context, HttpRequest request, HttpResponse response) {            
            client.RevokeSessionCookies();
        }
    }

    [DataContract]
    public class KeycloakOauthUserInfoResponse
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
        [DataMember]
        public List<string> roles { get; set; }
    }

}


