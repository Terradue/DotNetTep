using System;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Text;
using Terradue.Ldap;
using Terradue.Portal;

namespace Terradue.Tep {
    public class KeycloakFactory {

        IfyContext Context;
        public static string COOKIE_TOKEN_ACCESS = "KEYCLOAK_token_access";
		public static string COOKIE_TOKEN_REFRESH = "KEYCLOAK_token_refresh";
		public static string COOKIE_TOKEN_ID = "KEYCLOAK_token_id";
       
        public KeycloakFactory(IfyContext context){
            Context = context;
        }
        
        public OauthTokenResponse GetExchangeToken(string token){

			if (System.Configuration.ConfigurationManager.AppSettings["use_keycloak_exchange"] == null || 
				System.Configuration.ConfigurationManager.AppSettings["use_keycloak_exchange"] != "true") return null;

			var cookie = LoadTokenAccess();			
			var cookiesSeconds = cookie != null ? cookie.Expire.Subtract(DateTime.UtcNow).TotalSeconds : 0;
			if(cookie != null && cookiesSeconds > Context.GetConfigIntegerValue("AccessTokenExpireMinutes")) return new OauthTokenResponse();

            string client_id = System.Configuration.ConfigurationManager.AppSettings["keycloak_client_id"];
            string client_secret = System.Configuration.ConfigurationManager.AppSettings["keycloak_client_secret"];
            string subject_issuer = System.Configuration.ConfigurationManager.AppSettings["keycloak_subject_issuer"];
            string url = System.Configuration.ConfigurationManager.AppSettings["keycloak_token_endpoint"];
			string scope = System.Configuration.ConfigurationManager.AppSettings["keycloak_scope"];

            Context.LogDebug(Context, "GetExchangeToken - " + HttpContext.Current.Session.SessionID + " - expire at " + cookie.Expire.ToString());
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";

            var dataStr = string.Format("client_id={0}&client_secret={1}" + 
                    "&grant_type=urn:ietf:params:oauth:grant-type:token-exchange&subject_token={2}&subject_issuer={3}" +
                    "&subject_token_type=urn:ietf:params:oauth:token-type:access_token&audience={0}&scope={4}", 
                    client_id, client_secret, token, subject_issuer, scope);

            byte[] data = System.Text.Encoding.UTF8.GetBytes(dataStr);

            webRequest.ContentLength = data.Length;

            using (var requestStream = webRequest.GetRequestStream()) {
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                try {
                    var response = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse,
                                                                        webRequest.EndGetResponse,
                                                                        null)
                    .ContinueWith(task =>
                    {
                        var httpResponse = (HttpWebResponse) task.Result;
                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                            string result = streamReader.ReadToEnd();
                            try {
                                return JsonSerializer.DeserializeFromString<OauthTokenResponse>(result);
                            } catch (Exception e) {
                                throw e;
                            }
                        }
                    }).ConfigureAwait(false).GetAwaiter().GetResult();

                    if (response.access_token != null) StoreTokenAccess(response.access_token, Context.Username, response.expires_in);
                    if (response.refresh_token != null) StoreTokenRefresh(response.refresh_token, Context.Username);
                    if (response.id_token != null) StoreTokenId(response.id_token, Context.Username, response.expires_in);
                    Context.LogDebug(this, "Access Token valid " + response.expires_in + " seconds");
                    return response;						
                } catch (Exception e) {
                    DeleteTokenAccess();
                    DeleteTokenRefresh();
                    DeleteTokenId();
                    throw new Exception("Internal token exchange error");
                }
            }
        }

		public OauthTokenResponse RefreshToken(string username) {

			Context.LogDebug(this, "Keycloak refresh Token");
			Context.WriteInfo("Keycloak refresh Token");

			var token = LoadTokenRefresh(username);

			Context.WriteInfo(token.Value);

			if(string.IsNullOrEmpty(token.Value)){
				Context.LogError(this, "Keycloak RefreshToken error : empty refresh token");
				
				throw new Exception("Empty refresh token");
			}

			string client_id = System.Configuration.ConfigurationManager.AppSettings["keycloak_client_id"];
            string client_secret = System.Configuration.ConfigurationManager.AppSettings["keycloak_client_secret"];
            string subject_issuer = System.Configuration.ConfigurationManager.AppSettings["keycloak_subject_issuer"];
            string url = System.Configuration.ConfigurationManager.AppSettings["keycloak_token_endpoint"];
			string scope = System.Configuration.ConfigurationManager.AppSettings["keycloak_scope"];
			
			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
			webRequest.Method = "POST";
			webRequest.ContentType = "application/x-www-form-urlencoded";
			
			var dataStr = string.Format("grant_type=refresh_token&refresh_token={0}&client_id={1}&client_secret={2}&scope={3}", token.Value, client_id, client_secret, scope);

			byte[] data = System.Text.Encoding.UTF8.GetBytes(dataStr);
			webRequest.ContentLength = data.Length;

			using (var requestStream = webRequest.GetRequestStream()) {
				requestStream.Write(data, 0, data.Length);
				requestStream.Close();
				try {
					Context.LogDebug(this, "RefreshToken debug url =  " + webRequest.RequestUri.AbsoluteUri);
					var response = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse,
                                                                       webRequest.EndGetResponse,
                                                                       null)
					.ContinueWith(task =>
					{
						var httpResponse = (HttpWebResponse) task.Result;
						using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
							string result = streamReader.ReadToEnd();
							Context.LogDebug(this, "RefreshToken result =  " + result);
							Context.WriteInfo("RefreshToken result =  " + result);
							try {
								return JsonSerializer.DeserializeFromString<OauthTokenResponse>(result);
							} catch (Exception e) {
								throw e;
							}
						}
					}).ConfigureAwait(false).GetAwaiter().GetResult();

					Context.WriteInfo("RefreshToken result ok");
					
					if (response.access_token != null) StoreTokenAccess(response.access_token, username, response.expires_in, token.Session);
                    if (response.refresh_token != null) StoreTokenRefresh(response.refresh_token, username, token.Session);
                    if (response.id_token != null) StoreTokenId(response.id_token, username, response.expires_in, token.Session);
                    Context.LogDebug(this, "Access Token valid " + response.expires_in + " seconds");

					Context.WriteInfo("Access Token valid " + response.expires_in + " seconds");
                    
					return response;
						
				} catch (Exception e) {
					Context.LogError(this, "RefreshToken error : " + e.Message);					
					Context.WriteError(e.Message);
					throw e;
				}
			}
		}

        /// <summary>
		/// Loads the token access.
		/// </summary>
		/// <returns>The token access.</returns>
		public DBCookie LoadTokenAccess() {
            Context.LogDebug(this, "LoadTokenAccess - " + HttpContext.Current.Session.SessionID);
			return DBCookie.LoadDBCookie(Context, COOKIE_TOKEN_ACCESS);
		}

		/// <summary>
		/// Stores the token access.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <param name="expire">Expire.</param>
		public void StoreTokenAccess(string value, string username, long expire, string session = null) {
			if (HttpContext.Current != null && HttpContext.Current.Session != null){
				Context.LogDebug(this, "StoreTokenAccess - " + HttpContext.Current.Session.SessionID + " - " + username);
				DBCookie.StoreDBCookie(Context, COOKIE_TOKEN_ACCESS, value, username, expire);            
			} else {
				Context.LogDebug(this, "StoreTokenAccess - " + session + " - " + username);
				DBCookie.StoreDBCookie(Context, session, COOKIE_TOKEN_ACCESS, value, username, DateTime.UtcNow.AddSeconds(expire));
			}
		}

		/// <summary>
		/// Deletes the token access.
		/// </summary>
		public void DeleteTokenAccess() {
			DBCookie.DeleteDBCookie(Context, COOKIE_TOKEN_ACCESS);
		}

		/// <summary>
		/// Loads the token refresh.
		/// </summary>
		/// <returns>The token refresh.</returns>
		public DBCookie LoadTokenRefresh() {
			return DBCookie.LoadDBCookie(Context, COOKIE_TOKEN_REFRESH);
		}

		public DBCookie LoadTokenRefresh(string username) {
			return DBCookie.FromUsernameAndIdentifier(Context, username, COOKIE_TOKEN_REFRESH);
		}

		/// <summary>
		/// Stores the token refresh.
		/// </summary>
		/// <param name="value">Value.</param>
		public void StoreTokenRefresh(string value, string username, string session = null) {			
			if (HttpContext.Current != null && HttpContext.Current.Session != null){
				Context.LogDebug(this, "StoreTokenRefresh - " + HttpContext.Current.Session.SessionID + " - " + username);
				DBCookie.StoreDBCookie(Context, COOKIE_TOKEN_REFRESH, value, username);            
			} else {
				Context.LogDebug(this, "StoreTokenRefresh - " + session + " - " + username);
				DBCookie.StoreDBCookie(Context, session, COOKIE_TOKEN_REFRESH, value, username, DateTime.UtcNow.AddSeconds(86400L));
			}
		}

		/// <summary>
		/// Deletes the token refresh.
		/// </summary>
		public void DeleteTokenRefresh() {
			DBCookie.DeleteDBCookie(Context, COOKIE_TOKEN_REFRESH);
		}

		/// <summary>
		/// Loads the token id.
		/// </summary>
		/// <returns>The token id.</returns>
		public DBCookie LoadTokenId() {
			return DBCookie.LoadDBCookie(Context, COOKIE_TOKEN_ID);
		}

		/// <summary>
		/// Stores the token id.
		/// </summary>
		/// <param name="value">Value.</param>
		public void StoreTokenId(string value, string username, long expire, string session = null) {
			if (HttpContext.Current != null && HttpContext.Current.Session != null){
				Context.LogDebug(this, "StoreTokenId - " + HttpContext.Current.Session.SessionID + " - " + username);
				DBCookie.StoreDBCookie(Context, COOKIE_TOKEN_ID, value, username, expire);            
			} else {
				Context.LogDebug(this, "StoreTokenId - " + session + " - " + username);
				DBCookie.StoreDBCookie(Context, session, COOKIE_TOKEN_ID, value, username, DateTime.UtcNow.AddSeconds(expire));
			}

			if(Context.UserId != 0)	CookiesFactory.StoreSessionCookies(Context, value,DateTime.UtcNow.AddSeconds(expire));
		}

		/// <summary>
		/// Deletes the token id.
		/// </summary>
		public void DeleteTokenId() {
			DBCookie.DeleteDBCookie(Context, COOKIE_TOKEN_ID);
		}
    }
}