using System;
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
    }
}
