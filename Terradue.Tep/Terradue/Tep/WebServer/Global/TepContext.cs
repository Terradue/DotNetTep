using System;
using System.IO;
using System.Net;
using System.Web;
using Terradue.Portal;
using Terradue.Tep.Controller;

namespace Terradue.Tep.WebServer {

    /// <summary>
    /// TepQW web context.
    /// </summary>
    public class TepLocalContext : IfyLocalContext {
        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.Common.TepQWLocalContext"/> class.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <param name="baseUrl">Base URL.</param>
        /// <param name="applicationName">Application name.</param>
        public TepLocalContext(string connectionString, string baseUrl, string applicationName) : base(connectionString,baseUrl,applicationName) {}
        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.Common.TepQWLocalContext"/> class.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <param name="console">Console.</param>
        public TepLocalContext(string connectionString, bool console) : base(connectionString,console){}
    }

    /// <summary>
    /// TepQW web context.
    /// </summary>
    public class TepWebContext : IfyWebContext {

        public TepWebContext(PagePrivileges privileges) : base(privileges) {
            HideMessages = true;
            System.Configuration.Configuration rootWebConfig =
                System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(null);
            BaseUrl = rootWebConfig.AppSettings.Settings["BaseUrl"].Value;

            HttpContext.Current.Session.Timeout = 1440;
            this.DynamicDbConnectionsGlobal = true;
        }

        public static TepWebContext GetWebContext(PagePrivileges privileges){
            TepWebContext result = new TepWebContext (privileges);
            return result;
        }

        public override void Open (){
            base.Open ();
            if (UserLevel == Terradue.Portal.UserLevel.Administrator) AdminMode = true;
        }

        protected override void LoadAdditionalConfiguration() {
            base.LoadAdditionalConfiguration();
            System.Configuration.Configuration rootWebConfig =
                System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(null);
            BaseUrl = rootWebConfig.AppSettings.Settings["BaseUrl"].Value;
        } 

        public override bool CheckCanStartSession(User user, bool throwOnError) {
            if (user.AccountStatus == AccountStatusType.PendingActivation && GetConfigBooleanValue("PendingUserCanLogin")) return true;
            return base.CheckCanStartSession(user, throwOnError);
        }
    }
}

