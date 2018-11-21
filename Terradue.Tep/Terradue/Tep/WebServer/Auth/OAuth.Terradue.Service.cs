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

                redirect = string.Format("{0}?client_id={1}&response_type={2}&nonce={3}&state={4}&redirect_uri={5}&ajax={6}&scope={7}",
                                                 context.GetConfigValue("oauth-authEndpoint"),
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
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return DoRedirect(redirect, false);
        }

        public object Get(OauthCallBackRequest request) {

            var redirect = "";

            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);
            UserTep user = null;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/cb GET"));
                if (!string.IsNullOrEmpty(request.error)) {
                    context.LogError(this, request.error);
                    context.EndSession();
                    return DoRedirect(context.BaseUrl, false);
                }

                Connect2IdClient client = new Connect2IdClient(context.GetConfigValue("sso-configUrl"));
                client.SSOAuthEndpoint = context.GetConfigValue("sso-authEndpoint");
                client.SSOApiClient = context.GetConfigValue("sso-clientId");
                client.SSOApiSecret = context.GetConfigValue("sso-clientSecret");
                client.SSOApiToken = context.GetConfigValue("sso-apiAccessToken");
                client.RedirectUri = context.GetConfigValue("sso-callback");
                try {
                    var tokenresponse = client.AccessToken(request.Code);
                    DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-access"), tokenresponse.access_token, tokenresponse.expires_in);
                    DBCookie.StoreDBCookie(context, context.GetConfigValue("cookieID-token-refresh"), tokenresponse.refresh_token);
                } catch (Exception e) {
                    DBCookie.DeleteDBCookie(context, context.GetConfigValue("cookieID-token-access"));
                    DBCookie.DeleteDBCookie(context, context.GetConfigValue("cookieID-token-refresh"));
                    throw e;
                }

                TepLdapAuthenticationType auth = (TepLdapAuthenticationType)IfyWebContext.GetAuthenticationType(typeof(TepLdapAuthenticationType));
                auth.SetConnect2IdCLient(client);
                auth.TrustEmail = true;

                user = (UserTep)auth.GetUserProfile(context);
                if (user == null) throw new Exception("Unable to load user");
                context.LogDebug(this, string.Format("Loaded user '{0}'", user.Username));
                if (string.IsNullOrEmpty(user.Email)) throw new Exception("Invalid email");
                user.Store();

                context.StartSession(auth, user);
                context.SetUserInformation(auth, user);

                redirect = context.GetConfigValue("dashboard_page");

                if (!string.IsNullOrEmpty(HttpContext.Current.Session["return_to"] as string)) {
                    redirect = HttpContext.Current.Session["return_to"] as string;
                    HttpContext.Current.Session["return_to"] = null;
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }
            return DoRedirect(redirect, false);
        }

        public object Delete(OauthLogoutRequest request) {
            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/auth DELETE"));
                context.EndSession();
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }
            if (request.redirect_uri != null) return DoRedirect(request.redirect_uri, request.ajax);
            else return DoRedirect("/", request.ajax);
        }

        public object Get(OauthLogoutRequest request) {
            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/logout GET"));
                context.EndSession();
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }
            if (request.redirect_uri != null) return DoRedirect(request.redirect_uri, request.ajax);
            else return DoRedirect("/", request.ajax);
        }

        private HttpResult DoRedirect(string redirect, bool ajax) {
            if (ajax) {
                HttpResult redirectResponse = new HttpResult();
                var location = HttpContext.Current.Response.Headers[HttpHeaders.Location];
                if (string.IsNullOrEmpty(location) || !location.Equals(redirect))
                    redirectResponse.Headers[HttpHeaders.Location] = redirect;
                redirectResponse.StatusCode = System.Net.HttpStatusCode.NoContent;
                return redirectResponse;
            } else {
                HttpResult redirectResponse = new HttpResult();
                redirectResponse.Headers[HttpHeaders.Location] = redirect;
                redirectResponse.StatusCode = System.Net.HttpStatusCode.Redirect;
                return redirectResponse;
            }
        }

    }
}

