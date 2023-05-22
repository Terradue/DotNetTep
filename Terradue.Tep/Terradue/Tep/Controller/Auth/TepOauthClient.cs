using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ServiceStack.Text;
using Terradue.Ldap;
using Terradue.Portal;

namespace Terradue.Tep {
    
    public class TepOauthClient {

		public string RedirectUri { get; set; }
		public string AuthEndpoint { get; set; }
		public string TokenEndpoint { get; set; }
		public string UserInfoEndpoint { get; set; }
		public string LogoutEndpoint { get; set; }
		public string ClientName { get; set; }
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
		public string Scopes { get; set; }
		public string Callback { get; set; }
		public bool UseAuthorizationHeader { get; set; }

		public static string COOKIE_TOKEN_ACCESS = "SSO_token_access";
		public static string COOKIE_TOKEN_REFRESH = "SSO_token_refresh";
		public static string COOKIE_TOKEN_ID = "SSO_token_id";

		static System.Collections.Specialized.NameValueCollection AppSettings = System.Configuration.ConfigurationManager.AppSettings;		

		protected IfyContext Context;

		public TepOauthClient(IfyContext context) {
			Context = context;
			UseAuthorizationHeader = true;

			AuthEndpoint = context.GetConfigValue("sso-authEndpoint");
			ClientName = context.GetConfigValue("sso-clientName");
			ClientId = context.GetConfigValue("sso-clientId");
			ClientSecret = context.GetConfigValue("sso-clientSecret");
			TokenEndpoint = context.GetConfigValue("sso-tokenEndpoint");
			LogoutEndpoint = context.GetConfigValue("sso-logoutEndpoint");
			UserInfoEndpoint = context.GetConfigValue("sso-userInfoEndpoint");
			Callback = context.GetConfigValue("sso-callback");
			Scopes = context.GetConfigValue("sso-scopes");

			// ServicePointManager.ServerCertificateValidationCallback = delegate (
			// 	Object obj, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain,
			// 	System.Net.Security.SslPolicyErrors errors) { return (true); };
		}

		#region TOKEN

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
		public void StoreTokenAccess(string value, string username, long expire) {
			Context.LogDebug(this, "StoreTokenAccess - " + HttpContext.Current.Session.SessionID + " - " + username);
			DBCookie.StoreDBCookie(Context, COOKIE_TOKEN_ACCESS, value, username, expire);            
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

		/// <summary>
		/// Stores the token refresh.
		/// </summary>
		/// <param name="value">Value.</param>
		public void StoreTokenRefresh(string value, string username) {			
			Context.LogDebug(this, "StoreTokenRefresh - " + HttpContext.Current.Session.SessionID + " - " + username);
			DBCookie.StoreDBCookie(Context, COOKIE_TOKEN_REFRESH, value, username);
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
		public void StoreTokenId(string value, string username, long expire) {
			Context.LogDebug(this, "StoreTokenId - " + HttpContext.Current.Session.SessionID + " - " + username);
			DBCookie.StoreDBCookie(Context, COOKIE_TOKEN_ID, value, username, expire);
			if(Context.UserId != 0)	CookiesFactory.StoreSessionCookies(Context, value,DateTime.UtcNow.AddSeconds(expire));
		}

		/// <summary>
		/// Deletes the token id.
		/// </summary>
		public void DeleteTokenId() {
			DBCookie.DeleteDBCookie(Context, COOKIE_TOKEN_ID);
		}
		#endregion

		#region COOKIES

		public void RevokeAllCookies() {
			DeleteTokenAccess();
			DeleteTokenRefresh();
		}

		public void RevokeSessionCookies() {

			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(LogoutEndpoint);
			webRequest.Method = "POST";
			if (!string.IsNullOrEmpty(AppSettings["ProxyHost"])) webRequest.Proxy = GetWebRequestProxy();
			webRequest.ContentType = "application/x-www-form-urlencoded";
			webRequest.Headers["Authorization"] = GetBasicAuthenticationSecret();

			var dataStr = string.Format("token={0}&token_type_hint=access_token", LoadTokenAccess().Value);
			byte[] data = System.Text.Encoding.UTF8.GetBytes(dataStr);

			webRequest.ContentLength = data.Length;
			try {
				using (var requestStream = webRequest.GetRequestStream()) {
					requestStream.Write(data, 0, data.Length);
					requestStream.Close();
					System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse,
                                                                       webRequest.EndGetResponse,
                                                                       null)
					.ContinueWith(task =>
					{
						var httpResponse = (HttpWebResponse) task.Result;						
					}).ConfigureAwait(false).GetAwaiter().GetResult();
				}
			}catch(Exception e) {
				Context.LogError(this, e.Message);
            }

			DBCookie.RevokeSessionCookies(Context);			
			CookiesFactory.CleanSessionCookies(Context);
		}
		#endregion

		/// <summary>
		/// Gets the authorization URL.
		/// </summary>
		/// <returns>The authorization URL.</returns>
		public string GetAuthorizationUrl() {
			if (string.IsNullOrEmpty(AuthEndpoint)) throw new Exception("Invalid Authorization endpoint");

			var scope = Scopes.Replace(",", "%20");
			var redirect_uri = HttpUtility.UrlEncode(Callback);
			var query = string.Format("response_type={0}&scope={1}&client_id={2}&state={3}&redirect_uri={4}&nonce={5}",
										  "code", scope, ClientId, Guid.NewGuid().ToString(), redirect_uri, Guid.NewGuid().ToString());

			string url = string.Format("{0}?{1}", AuthEndpoint, query);

			return url;
		}

        private IWebProxy GetWebRequestProxy() {
            if (!string.IsNullOrEmpty(AppSettings["ProxyHost"])) {
                if (!string.IsNullOrEmpty(AppSettings["ProxyPort"]))
                    return new WebProxy(AppSettings["ProxyHost"], int.Parse(AppSettings["ProxyPort"]));
                else
                    return new WebProxy(AppSettings["ProxyHost"]);
            } else
                return null;
        }

        /// <summary>
        /// Accesses the token.
        /// </summary>
        /// <param name="code">Code.</param>
        public OauthTokenResponse AccessToken(string code) {
			Context.LogDebug(this, "AccessToken - " + HttpContext.Current.Session.SessionID);
			var scope = Scopes.Replace(",", "%20");
			string url = string.Format("{0}", TokenEndpoint);
			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
			webRequest.Method = "POST";
			webRequest.ContentType = "application/x-www-form-urlencoded";
			if (!string.IsNullOrEmpty(AppSettings["ProxyHost"])) webRequest.Proxy = GetWebRequestProxy();

			var dataStr = string.Format("grant_type=authorization_code&redirect_uri={0}&code={1}&scope={2}", HttpUtility.UrlEncode(Callback), code, scope);

			if (this.UseAuthorizationHeader) {
				webRequest.Headers.Add(HttpRequestHeader.Authorization, GetBasicAuthenticationSecret());
			} else {
				dataStr += string.Format("&client_id={0}&client_secret={1}", this.ClientId, this.ClientSecret);
			}

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

					if (response.access_token != null) StoreTokenAccess(response.access_token, "", response.expires_in);
					if (response.refresh_token != null) StoreTokenRefresh(response.refresh_token, "");
					if (response.id_token != null) StoreTokenId(response.id_token, "", response.expires_in);
					Context.LogDebug(this, "Access Token valid " + response.expires_in + " seconds");

					try{
						if (System.Configuration.ConfigurationManager.AppSettings["use_keycloak_exchange"] != null && System.Configuration.ConfigurationManager.AppSettings["use_keycloak_exchange"] == "true")
						{
							var cookie = DBCookie.LoadDBCookie(Context, COOKIE_TOKEN_ACCESS);
							var kfact = new KeycloakFactory(Context);
							kfact.GetExchangeToken(cookie.Value);
						}
					}catch(Exception e){}

					return response;						
				} catch (Exception e) {
					DeleteTokenAccess();
					DeleteTokenRefresh();
					DeleteTokenId();
					throw e;
				}
			}
		}

		/// <summary>
		/// Refreshs the token.
		/// </summary>
		/// <param name="token">Token.</param>
		public OauthTokenResponse RefreshToken(string token, string username, bool store = true) {

			var scope = Scopes.Replace(",", "%20");
			string url = string.Format("{0}", TokenEndpoint);
			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
			webRequest.Method = "POST";
			webRequest.ContentType = "application/x-www-form-urlencoded";
			if (!string.IsNullOrEmpty(AppSettings["ProxyHost"])) webRequest.Proxy = GetWebRequestProxy();

			var dataStr = string.Format("grant_type=refresh_token&refresh_token={0}&scope={1}", token, scope);

			if (this.UseAuthorizationHeader) {
				webRequest.Headers.Add(HttpRequestHeader.Authorization, GetBasicAuthenticationSecret());
			} else {
				dataStr += string.Format("&client_id={0}&client_secret={1}", this.ClientId, this.ClientSecret);
			}

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
							try {
								return JsonSerializer.DeserializeFromString<OauthTokenResponse>(result);
							} catch (Exception e) {
								throw e;
							}
						}
					}).ConfigureAwait(false).GetAwaiter().GetResult();
					
					if(store){
						DBCookie.DeleteDBCookies(Context, HttpContext.Current.Session.SessionID);
						StoreTokenAccess(response.access_token, username, response.expires_in);
						StoreTokenRefresh(response.refresh_token, username);
						if(!string.IsNullOrEmpty(response.id_token)) StoreTokenId(response.id_token, username, response.expires_in);
					}
					return response;
						
				} catch (Exception e) {
					Context.LogError(this, "RefreshToken error : " + e.Message);
					RevokeSessionCookies();					
					throw e;
				}
			}
		}

		/// <summary>
		/// Gets the user info.
		/// </summary>
		/// <returns>The user info.</returns>
		/// <param name="token">Token.</param>
		public T GetUserInfo<T>(string token) {

			T user;
			string url = string.Format("{0}", UserInfoEndpoint);
			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
			webRequest.Method = "GET";
			webRequest.ContentType = "application/json";
			if (!string.IsNullOrEmpty(AppSettings["ProxyHost"])) webRequest.Proxy = GetWebRequestProxy();
			webRequest.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);

			user = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse,
                                                                       webRequest.EndGetResponse,
                                                                       null)
			.ContinueWith(task =>
			{
				var httpResponse = (HttpWebResponse) task.Result;
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
					string result = streamReader.ReadToEnd();
					try {
						Context.LogInfo(this, result);
						return JsonSerializer.DeserializeFromString<T>(result);
					} catch (Exception e) {
						throw e;
					}
				}
			}).ConfigureAwait(false).GetAwaiter().GetResult();

			return user;
		}

		protected string GetBasicAuthenticationSecret() {
			return "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(this.ClientId + ":" + this.ClientSecret));
		}

		public virtual string GetLogoutUrl() {
			//return string.Format("{0}", LogoutEndpoint);
			return string.Format("{0}?token={1}", LogoutEndpoint, LoadTokenId().Value);
		}
    }

	public static class MyArrayExtensions {
		/// <summary>
		/// Splits an array into several smaller arrays.
		/// </summary>
		/// <typeparam name="T">The type of the array.</typeparam>
		/// <param name="array">The array to split.</param>
		/// <param name="size">The size of the smaller arrays.</param>
		/// <returns>An array containing smaller arrays.</returns>
		public static IEnumerable<IEnumerable<T>> Split<T>(this T[] array, int size) {
			for (var i = 0; i < (float)array.Length / size; i++) {
				yield return array.Skip(i * size).Take(size);
			}
		}
	}
}
