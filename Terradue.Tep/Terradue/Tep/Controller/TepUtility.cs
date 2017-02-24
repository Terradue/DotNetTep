using System;
namespace Terradue.Tep {
    public class TepUtility {

        public static string GenerateIdentifier(string identifier) {
            if (string.IsNullOrEmpty(identifier)) return "";
            string result = identifier.Replace(" ", "").Replace(".", "").Replace("?", "").Replace("&", "").Replace("%", "");
            return result;
        }
    }
}
