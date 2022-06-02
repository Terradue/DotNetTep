using System;
using ServiceStack.Common.Web;
using System.Web;
using System.Security.Cryptography;
using Terradue.Ldap;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Terradue.Tep {
    public class OAuthUtils {

        public static HttpResult DoRedirect(string redirect, bool ajax) {
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
        
        public static string GetQueryString(NameValueCollection nvc) {
            var queryString = Array.ConvertAll(nvc.AllKeys, key => string.Format("{0}={1}", key, nvc[key]));
            return string.Join("&", queryString);
        }

    }
}
