using System;
using System.Security.Cryptography;

namespace Terradue.Tep {
    public class TepUtility {

        /// <summary>
        /// Validates the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="identifier">Identifier.</param>
        public static string ValidateIdentifier(string identifier) {
            if (string.IsNullOrEmpty(identifier)) return "";
            string result = identifier.Replace(" ", "").Replace(".", "").Replace("?", "").Replace("&", "").Replace("%", "").Replace("#", "");
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
    }
}
