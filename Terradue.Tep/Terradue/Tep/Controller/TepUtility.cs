using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Net;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Terradue.Tep {
    public class TepUtility {

        /// <summary>
        /// Validates the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="identifier">Identifier.</param>
        public static string ValidateIdentifier(string identifier) {
            if (string.IsNullOrEmpty(identifier)) return "";
            string result = identifier.Replace(" ", "").Replace(".", "").Replace("?", "").Replace("&", "").Replace("%", "").Replace("#", "").Replace("/","").Replace("\\", "");
            return result;
        }

		public static string HashHMAC(string key, string msg) {
			var encoding = new System.Text.ASCIIEncoding();
			var bkey = encoding.GetBytes(key);
			var bmsg = encoding.GetBytes(msg);
			var hash = new HMACSHA256(bkey);
			var hashmac = hash.ComputeHash(bmsg);
			return BitConverter.ToString(hashmac).Replace("-", "").ToLower();
		}

        public static string RemoveAccents(string text){
            if(string.IsNullOrEmpty(text)) return text;
            try
            {
                var sbReturn = new System.Text.StringBuilder();
                var arrayText = text.Normalize(System.Text.NormalizationForm.FormD).ToCharArray();
                foreach (char letter in arrayText)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
                        sbReturn.Append(letter);
                }
                return sbReturn.ToString();
            }catch(Exception e){
                return text;
            }
        }  

        public static IWebProxy GetWebRequestProxy() {
            if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["ProxyHost"])) {
                if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["ProxyPort"]))
                    return new WebProxy(System.Configuration.ConfigurationManager.AppSettings["ProxyHost"], int.Parse(System.Configuration.ConfigurationManager.AppSettings["ProxyPort"]));
                else
                    return new WebProxy(System.Configuration.ConfigurationManager.AppSettings["ProxyHost"]);
            } else
                return null;
        }
    }

     public static class TemplateExtensions
    {
        public static string ReplaceMacro<T>(this string template, string key, T obj)
        {
            return Regex.Replace(template, @"\${(?<exp>[^}]+)}", match =>
            {
                var p = Expression.Parameter(typeof(T), key);
                var e = System.Linq.Dynamic.Core.DynamicExpressionParser.ParseLambda( new[] { p }, typeof(object), match.Groups["exp"].Value);
                return (e.Compile().DynamicInvoke(obj) ?? "").ToString();
            });
        }
    }
}
