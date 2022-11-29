using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ServiceStack.Text;
using Terradue.Ldap;
using Terradue.Portal;

namespace Terradue.Tep {
    public class CookiesFactory {
        public static void CleanSessionCookies(IfyContext context) {
			var cookiechunk = HttpContext.Current.Request.Cookies[context.GetConfigValue("sso-session-cookie") + "-chunk"];
			if (cookiechunk != null && !string.IsNullOrEmpty(cookiechunk.Value)) {
				int j = int.Parse(cookiechunk.Value);
				for (int i = 1; i <= j; i++) {
					var cookie = HttpContext.Current.Request.Cookies[context.GetConfigValue("sso-session-cookie") + i];
					cookie.Expires = DateTime.UtcNow.AddDays(-10);
					cookie.Value = null;
					HttpContext.Current.Response.SetCookie(cookie);
				}
				cookiechunk.Expires = DateTime.UtcNow.AddDays(-10);
				cookiechunk.Value = null;
				HttpContext.Current.Response.SetCookie(cookiechunk);
			}
		}

		public static void StoreSessionCookies(IfyContext context, string value, DateTime expire){
			if (context.GetConfigBooleanValue("sso-cookie-disable")) return;
			if (expire < DateTime.UtcNow) return;
			if (string.IsNullOrEmpty(value)) return;
			if (context.GetConfigIntegerValue("sso-session-cookie-maxsize") != 0 && value.Length > context.GetConfigIntegerValue("sso-session-cookie-maxsize")) return; //browsers cannot handle too big cookies
			try{
				CleanSessionCookies(context);//to remove old cookies
			}catch(Exception){}
			var cookieVal = Encrypt(value, context.GetConfigValue("sso-session-cookie-key"));

			context.LogDebug(context,string.Format("StoreSessionCookies - {0} - {1} - {2}", context.Username, expire.ToString("yyyy-MM-ddThh:mm:ss"), value));

			var bytes = System.Text.ASCIIEncoding.Unicode.GetByteCount(cookieVal);
			int remainder;
			int quotient = Math.DivRem(bytes, 4000, out remainder);
			var count = remainder == 0 ? quotient : quotient + 1;
			int quotient2 = Math.DivRem(cookieVal.Length, count, out remainder);
			int chunksize = remainder == 0 ? quotient2 : quotient2 + 1;

			var carray = cookieVal.ToCharArray();
			IEnumerable<IEnumerable<char>> splited = carray.Split<char>(chunksize);
			int j = 0;
			foreach (IEnumerable<char> s in splited) {
				j++;
				var cookieChunkString = StringBuilderChars(s);
				var cookie = new HttpCookie(context.GetConfigValue("sso-session-cookie") + j, cookieChunkString);
				cookie.Domain = context.GetConfigValue("sso-session-cookie-domain");
				cookie.Expires = expire;
				HttpContext.Current.Response.SetCookie(cookie);
			}
			var cookiechunk = new HttpCookie(context.GetConfigValue("sso-session-cookie") + "-chunk", j + "");
			cookiechunk.Domain = context.GetConfigValue("sso-session-cookie-domain");
			cookiechunk.Expires = expire;
			HttpContext.Current.Response.SetCookie(cookiechunk);
		}

		static string StringBuilderChars(IEnumerable<char> charSequence) {
			var sb = new StringBuilder();
			foreach (var c in charSequence) {
				sb.Append(c);
			}
			return sb.ToString();
		}

		public static string Encrypt(string value, string key) {
			RijndaelManaged rDel = new RijndaelManaged();
			byte[] keyIV = UTF8Encoding.UTF8.GetBytes("");			
            var keyArray = UTF8Encoding.UTF8.GetBytes(key);
			var toEncryptArray = UTF8Encoding.UTF8.GetBytes(value);
			rDel.Key = keyArray;
			rDel.KeySize = 128;
			rDel.Mode = CipherMode.ECB;
			rDel.Padding = PaddingMode.PKCS7;
			ICryptoTransform cTransform = rDel.CreateEncryptor(keyArray, null);
			byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
			var encrypt64 = Convert.ToBase64String(resultArray);
			return encrypt64;
		}

		public static string Decrypt(string value, string key) {
			RijndaelManaged rDel = new RijndaelManaged();
			byte[] keyIV = UTF8Encoding.UTF8.GetBytes("");			
			var keyArray = UTF8Encoding.UTF8.GetBytes(key);
			var toDecryptArray = Convert.FromBase64String(value);
			rDel.Key = keyArray;
			rDel.KeySize = 128;
			rDel.Mode = CipherMode.ECB;
			rDel.Padding = PaddingMode.PKCS7;
			ICryptoTransform cTransform = rDel.CreateDecryptor(keyArray, null);
			byte[] resultArray = cTransform.TransformFinalBlock(toDecryptArray, 0, toDecryptArray.Length);
			var decrypt = UTF8Encoding.UTF8.GetString(resultArray);
			return decrypt;
		}    
    }
}