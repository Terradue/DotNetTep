using System;
using Terradue.Portal;

namespace Terradue.Tep {
    public class TermsConditionsHelper {

        /// <summary>
        /// Check if current user already agreed Terms and Conditions
        /// </summary>
        /// <param name="context"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static bool CheckTermsConditionsForCurrentUser(IfyContext context, string identifier) {
            string sql = string.Format("SELECT count(*) FROM termsconditions WHERE identifier='{0}' AND id_usr={1};", identifier, context.UserId);
            return context.GetQueryIntegerValue(sql) > 0;
        }

        /// <summary>
        /// Add a Terms & Conditions approval for current user
        /// </summary>
        /// <param name="context"></param>
        /// <param name="identifier"></param>
        public static void ApproveTermsConditionsForCurrentUser(IfyContext context, string identifier) {
            string sql = string.Format("INSERT INTO termsconditions (identifier,id_usr) VALUES ('{0}',{1});", identifier, context.UserId);
            try {
                context.Execute(sql);
            }catch(Exception e) {
                //T&C already exists
            }
        }
    }
}
