using System;
using System.Web;
using Terradue.Authentication.Ldap;
using Terradue.Ldap;
using Terradue.Portal;

namespace Terradue.Tep {
    public class TepLdapAuthenticationType : LdapAuthenticationType {

        protected bool NewUserCreated { get; set; }

        public TepLdapAuthenticationType(IfyContext context) : base(context) {}

        public override User GetUserProfile(IfyWebContext context, HttpRequest request = null, bool strict = false) {

            NewUserCreated = false;

            UserTep usr = null;
            AuthenticationType authType = IfyWebContext.GetAuthenticationType(typeof(TepLdapAuthenticationType));

            var tokenrefresh = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-refresh"));
            var tokenaccess = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-access"));

            context.LogDebug(this, string.Format("GetUserProfile -- tokenrefresh = {0} ; tokenaccess = {1}", tokenrefresh.Value, tokenaccess.Value));

            if (!string.IsNullOrEmpty(tokenrefresh.Value) && DateTime.UtcNow > tokenaccess.Expire) {
                // refresh the token
                try {
                    client.RefreshToken(tokenrefresh.Value);
                    tokenaccess = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-access"));
                    context.LogDebug(this, string.Format("GetUserProfile - refresh -- tokenrefresh = {0} ; tokenaccess = {1}", tokenrefresh.Value, tokenaccess.Value));
                } catch (Exception) {
                    return null;
                }
            }

            if (!string.IsNullOrEmpty(tokenaccess.Value)) {

                OauthUserInfoResponse usrInfo = client.GetUserInfo(tokenaccess.Value);

                context.LogDebug(this, string.Format("GetUserProfile -- usrInfo"));

                if (usrInfo == null) return null;

                context.LogDebug(this, string.Format("GetUserProfile -- usrInfo = {0}", usrInfo.sub));

                //Check if association auth / username exists
                int userId = User.GetUserId(context, usrInfo.sub, authType);
                bool userHasAuthAssociated = userId != 0;

                //user has ldap auth associated to his account
                if (userHasAuthAssociated) {
                    //User exists, we load it
                    usr = UserTep.FromId(context, userId);
                    return usr;
                }

                if (string.IsNullOrEmpty(usrInfo.email)) throw new Exception("Null email returned by the Oauth mechanism, please contact support.");

                //user does not have ldap auth associated to his account
                try {
                    //check if a user with the same email exists
                    usr = UserTep.FromEmail(context, usrInfo.email);

                    //user with the same email exists but not yet associated to ldap auth
                    usr.LinkToAuthenticationProvider(authType, usrInfo.sub);

                    return usr;
                    //TODO: what about if user Cloud username is different ? force to new one ?
                } catch (Exception e) {
                    context.LogError(this, e.Message);
                }

                //user with this email does not exist, we should create it
                usr = (UserTep)User.GetOrCreate(context, usrInfo.sub, authType);
                usr.Level = UserCreationDefaultLevel;

                //update user infos
                if (!string.IsNullOrEmpty(usrInfo.given_name))
                    usr.FirstName = usrInfo.given_name;
                if (!string.IsNullOrEmpty(usrInfo.family_name))
                    usr.LastName = usrInfo.family_name;
                if (!string.IsNullOrEmpty(usrInfo.email) && (TrustEmail || usrInfo.email_verifier))
                    usr.Email = usrInfo.email;
                if (!string.IsNullOrEmpty(usrInfo.zoneinfo))
                    usr.TimeZone = usrInfo.zoneinfo;
                if (!string.IsNullOrEmpty(usrInfo.locale))
                    usr.Language = usrInfo.locale;

                if (usr.Id == 0) {
                    usr.AccessLevel = EntityAccessLevel.Administrator;
                    NewUserCreated = true;
                }
                usr.Store();

                usr.LinkToAuthenticationProvider(authType, usrInfo.sub);

                usr.TerradueCloudUsername = usrInfo.sub;
                usr.StoreCloudUsername();

                return usr;
            } else {

            }

            context.LogDebug(this, string.Format("GetUserProfile -- return null"));

            return null;
        }
    }
}
