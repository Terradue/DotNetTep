using System;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Ldap;
using ServiceStack.Common.Web;
using System.Web;
using Terradue.Authentication.Ldap;

namespace Terradue.Tep.WebServer.Services {

    [Route("/oauth", "GET")]
    public class LoginRequest {
        [ApiMember(Name = "code", Description = "oauth code", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Code { get; set; }

        [ApiMember(Name = "state", Description = "oauth state", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string State { get; set; }

        [ApiMember(Name = "ajax", Description = "ajax", ParameterType = "path", DataType = "bool", IsRequired = true)]
        public bool ajax { get; set; }

        [ApiMember(Name = "error", Description = "error", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string error { get; set; }

        [ApiMember(Name = "return_to", Description = "return_to", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string return_to { get; set; }
    }

    [Route("/cb", "GET")]
    public class CallBackRequest {
        [ApiMember(Name = "code", Description = "oauth code", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Code { get; set; }

        [ApiMember(Name = "state", Description = "oauth state", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string State { get; set; }

        [ApiMember(Name = "ajax", Description = "ajax", ParameterType = "path", DataType = "bool", IsRequired = true)]
        public bool ajax { get; set; }

        [ApiMember(Name = "error", Description = "error", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string error { get; set; }
    }

    [Route("/oauth/cb", "GET")]
    public class OauthCallBackRequest {
        [ApiMember(Name = "code", Description = "oauth code", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Code { get; set; }

        [ApiMember(Name = "state", Description = "oauth state", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string State { get; set; }

        [ApiMember(Name = "ajax", Description = "ajax", ParameterType = "path", DataType = "bool", IsRequired = true)]
        public bool ajax { get; set; }

        [ApiMember(Name = "error", Description = "error", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string error { get; set; }
    }

    [Route("/logout", "GET", Summary = "logout", Notes = "Logout from the platform")]
    [Route("/auth", "DELETE", Summary = "logout", Notes = "Logout from the platform")]
    public class OauthLogoutRequest : IReturn<String> {
        [ApiMember(Name = "ajax", Description = "ajax", ParameterType = "path", DataType = "bool", IsRequired = false)]
        public bool ajax { get; set; }

        [ApiMember(Name = "redirect_uri", Description = "Redirect uri", ParameterType = "path", DataType = "String", IsRequired = false)]
        public String redirect_uri { get; set; }
    }

    [Api("Terradue Corporate webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    /// <summary>
    /// OAuth service. Used to log into the system
    /// </summary>
    public class OAuthServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(LoginRequest request) {
            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);

            string redirect = context.BaseUrl;
            try {
                context.Open();
                context.LogInfo(this, "/login GET");
                var client = new Connect2IdClient(context.GetConfigValue("sso-configUrl"));
                client.SSOAuthEndpoint = context.GetConfigValue("sso-authEndpoint");
                client.SSOApiClient = context.GetConfigValue("sso-clientId");
                client.SSOApiSecret = context.GetConfigValue("sso-clientSecret");
                client.SSOApiToken = context.GetConfigValue("sso-apiAccessToken");

                if (!string.IsNullOrEmpty(request.return_to)) HttpContext.Current.Session["return_to"] = request.return_to;

                var nonce = Guid.NewGuid().ToString();
                HttpContext.Current.Session["oauth-nonce"] = nonce;

                var scope = context.GetConfigValue("sso-scopes").Replace(",", "%20");
                var oauthEndpoint = context.GetConfigValue("oauth-authEndpoint");
                redirect = string.Format("{0}{1}client_id={2}&response_type={3}&nonce={4}&state={5}&redirect_uri={6}&ajax={7}&scope={8}",
                                         oauthEndpoint, 
                                         oauthEndpoint.Contains("?") ? "&" : "?",
                                         context.GetConfigValue("sso-clientId"),
                                         "code",
                                         nonce,
                                         Guid.NewGuid().ToString(),
                                         context.GetConfigValue("sso-callback"),
                                         "false",
                                         scope
                                        );

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }

            return OAuthUtils.DoRedirect(redirect, false);
        }

        public object Get(CallBackRequest request) {

            var redirect = "";

            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);
            UserTep user = null;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/cb GET"));
                if (!string.IsNullOrEmpty(request.error)) {
                    context.LogError(this, request.error);
                    context.EndSession();
                    return OAuthUtils.DoRedirect(context.BaseUrl, false);
                }

                Connect2IdClient client = new Connect2IdClient(context.GetConfigValue("sso-configUrl"));
                client.SSOAuthEndpoint = context.GetConfigValue("sso-authEndpoint");
                client.SSOApiClient = context.GetConfigValue("sso-clientId");
                client.SSOApiSecret = context.GetConfigValue("sso-clientSecret");
                client.SSOApiToken = context.GetConfigValue("sso-apiAccessToken");
                client.RedirectUri = context.GetConfigValue("sso-callback");
                OauthTokenResponse tokenresponse;
                try {
                    tokenresponse = client.AccessToken(request.Code);                    
                    DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-access"), tokenresponse.access_token, null, tokenresponse.expires_in);
                    DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-refresh"), tokenresponse.refresh_token, null);
                    DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-id"), tokenresponse.id_token, null, tokenresponse.expires_in);
                } catch (Exception e) {
                    DBCookie.DeleteDBCookie(context, context.GetConfigValue("cookieID-token-access"));
                    DBCookie.DeleteDBCookie(context, context.GetConfigValue("cookieID-token-refresh"));
                    DBCookie.DeleteDBCookie(context, context.GetConfigValue("cookieID-token-id"));
                    throw e;
                }

                TepLdapAuthenticationType auth = (TepLdapAuthenticationType)IfyWebContext.GetAuthenticationType(typeof(TepLdapAuthenticationType));
                auth.SetConnect2IdCLient(client);
                auth.TrustEmail = true;

                user = (UserTep)auth.GetUserProfile(context);
                if (user == null) throw new Exception("Unable to load user");
                context.LogDebug(this, string.Format("Loaded user '{0}'", user.Username));
                if (string.IsNullOrEmpty(user.Email)) throw new Exception("Invalid email");

                context.StartSession(auth, user);
                context.SetUserInformation(auth, user);

                DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-access"), tokenresponse.access_token, user.Username, tokenresponse.expires_in);
                DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-refresh"), tokenresponse.refresh_token, user.Username);
                DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-id"), tokenresponse.id_token, user.Username, tokenresponse.expires_in);

                redirect = context.GetConfigValue("dashboard_page");
                if(string.IsNullOrEmpty(redirect)) redirect = context.GetConfigValue("BaseUrl");

                if (!string.IsNullOrEmpty(HttpContext.Current.Session["return_to"] as string)) {
                    redirect = HttpContext.Current.Session["return_to"] as string;
                    HttpContext.Current.Session["return_to"] = null;
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return OAuthUtils.DoRedirect(redirect, false);
        }

        public object Get(OauthCallBackRequest request) {
            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);
            HttpResult redirect = null;
            User user = null;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/oauth/cb GET"));

                if (!string.IsNullOrEmpty(request.error)) {
                    context.LogError(this, request.error);
                    context.EndSession();
                    var baseUrl = context.BaseUrl;
                    context.Close();
                    return OAuthUtils.DoRedirect(baseUrl, false);
                }

                context.LogDebug(this, string.Format("Get token from code"));
                TepOauthAuthenticationType auth = new TepOauthAuthenticationType(context);          
                var client = auth.Client;
                var tokenResponse = client.AccessToken(request.Code);

                context.LogDebug(this, string.Format("Get user profile"));
                user = auth.GetUserProfile(context);

                if (tokenResponse.access_token != null) client.StoreTokenAccess(tokenResponse.access_token, user.Username, tokenResponse.expires_in);
                if (tokenResponse.refresh_token != null) client.StoreTokenRefresh(tokenResponse.refresh_token, user.Username);
                if (tokenResponse.id_token != null) client.StoreTokenId(tokenResponse.id_token, user.Username, tokenResponse.expires_in);                

                if (user == null) {
                    context.LogError(this, string.Format("Error to load user"));
                    var uri = new UriBuilder(context.GetConfigValue("BaseUrl"));
                    uri.Path = "/";
                    uri.Query = "error=login";
                    redirect = OAuthUtils.DoRedirect(uri.Uri.AbsoluteUri, false);
                } else {
                    context.LogDebug(this, string.Format("Loaded user '{0}'", user.Username));

                    context.StartSession(auth, user);
                    context.SetUserInformation(auth, user);

                    if (string.IsNullOrEmpty(HttpContext.Current.Session["return_to"] as string))
                        HttpContext.Current.Session["return_to"] = context.GetConfigValue("BaseUrl");

                    redirect = OAuthUtils.DoRedirect(HttpContext.Current.Session["return_to"] as string, false);
                }
                HttpContext.Current.Session["return_to"] = null;

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                redirect = OAuthUtils.DoRedirect(context.GetConfigValue("BaseUrl"), false);
            }
            return redirect;
        }

        public object Delete(OauthLogoutRequest request) {
            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/auth DELETE"));
                context.EndSession();
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            if (request.redirect_uri != null) return OAuthUtils.DoRedirect(request.redirect_uri, request.ajax);
            else return OAuthUtils.DoRedirect(context.GetConfigValue("BaseUrl"), request.ajax);
        }

        public object Get(OauthLogoutRequest request) {
            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/logout GET"));
                context.EndSession();
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            if (request.redirect_uri != null) return OAuthUtils.DoRedirect(request.redirect_uri, request.ajax);
            else return OAuthUtils.DoRedirect(context.GetConfigValue("BaseUrl"), request.ajax);
        }

    }
}

