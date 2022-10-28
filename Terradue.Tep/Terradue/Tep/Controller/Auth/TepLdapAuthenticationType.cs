﻿using System;
using System.Web;
using Terradue.Authentication.Ldap;
using Terradue.Ldap;
using Terradue.Portal;

namespace Terradue.Tep {
    public class TepLdapAuthenticationType : LdapAuthenticationType {

        protected bool NewUserCreated { get; set; }

        public TepLdapAuthenticationType(IfyContext context) : base(context) {}

        public void CheckRefresh() {
            var tokenaccess = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-access"));
            TimeSpan span = tokenaccess.Expire.Subtract(DateTime.UtcNow);
            if (span.TotalMinutes < context.GetConfigIntegerValue("AccessTokenExpireMinutes")) {
                if (span.TotalMinutes < 0) {
                    throw new Exception("Token is not valid anymore");
                } else {
                    var tokenrefresh = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-refresh"));
                    var tokenresponse = client.RefreshToken(tokenrefresh.Value);
                    DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-access"), tokenresponse.access_token, context.Username, tokenresponse.expires_in);
                    DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-refresh"), tokenresponse.refresh_token, context.Username);
                    if (!string.IsNullOrEmpty(tokenresponse.id_token)) DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-id"), tokenresponse.id_token, context.Username, tokenresponse.expires_in);
                }
            }
        }

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
                    var tokenresponse = client.RefreshToken(tokenrefresh.Value);
                    DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-access"), tokenresponse.access_token, tokenaccess.Username, tokenresponse.expires_in);
                    DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-refresh"), tokenresponse.refresh_token, tokenrefresh.Username);
                    DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-id"), tokenresponse.id_token, tokenrefresh.Username, tokenresponse.expires_in);
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
                    //test if TerradueCloudUsername was set
                    if (string.IsNullOrEmpty(usr.TerradueCloudUsername)) {
                        usr.LoadCloudUsername();
                        if (string.IsNullOrEmpty(usr.TerradueCloudUsername)) {
                            usr.TerradueCloudUsername = usrInfo.sub;
                            usr.StoreCloudUsername();
                        }
                    }

                    //update user infos
                    if (!string.IsNullOrEmpty(usrInfo.given_name))
                        usr.FirstName = usrInfo.given_name;
                    if (!string.IsNullOrEmpty(usrInfo.family_name))
                        usr.LastName = usrInfo.family_name;
                    if (!string.IsNullOrEmpty(usrInfo.zoneinfo))
                        usr.TimeZone = usrInfo.zoneinfo;
                    if (!string.IsNullOrEmpty(usrInfo.locale))
                        usr.Language = usrInfo.locale;

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

        public override void EndExternalSession(IfyWebContext context, HttpRequest request, HttpResponse response) {
            try{
                base.EndExternalSession(context, request, response);
            }catch(Exception e){
                context.LogError(this, e.Message);
            }
            try{
                HttpContext.Current.Session["t2apikey"] = null;
                HttpContext.Current.Session["t2loading"] = null;
                HttpContext.Current.Session["t2profileError"] = null;
            }catch(Exception e){
                context.LogError(this, e.Message);
            }
        }
    }
}
