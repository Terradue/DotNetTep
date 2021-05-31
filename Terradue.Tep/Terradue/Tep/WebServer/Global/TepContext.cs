using System;
using System.IO;
using System.Net;
using System.Web;
using Terradue.Portal;

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
            if (UserLevel == Terradue.Portal.UserLevel.Administrator) AccessLevel = EntityAccessLevel.Administrator;
            if (UserLevel > Terradue.Portal.UserLevel.Everybody) {

                if (this.UserInformation != null && this.UserInformation.AuthenticationType is TepLdapAuthenticationType) {

                    //check the validity of access token
                    try {
                        var auth = new TepLdapAuthenticationType(this);
                        auth.CheckRefresh();
                    } catch (Exception e) {
                        LogError(this, e.Message);
                        EndSession();//user token is not valid, we logout
                    }
                }
            }
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

        public override void LogError(object reporter, string message){
            PrepareLogger(reporter);
            base.LogError(reporter, message);
        }

        public override void LogDebug(object reporter, string message){
            PrepareLogger(reporter);
            base.LogDebug(reporter, message);
        }

        public override void LogInfo(object reporter, string message){
            PrepareLogger(reporter);
            base.LogInfo(reporter, message);
        }

        private void PrepareLogger(object reporter){
            log4net.GlobalContext.Properties["tepuser"] = this.Username != null ? this.Username : "Not logged user";
//            try{
//                switch (reporter.GetType().Name) {
//                    //CATALOGUE
//                    case "Terradue.Tep.WebServer.Services.CatalogueServiceTep":
//                    case "Terradue.Tep.WebServer.Services.CollectionServiceTep":
//                    case "Terradue.Tep.WebServer.Services.DataPackageServiceTep":
//                        log4net.GlobalContext.Properties["teplogtype"] = "Catalogue";
//                        break;
//
//                    //USER
//                    case "Terradue.Tep.WebServer.Services.UserServiceTep":
//                    case "Terradue.Tep.WebServer.Services.EmailConfirmServiceTep":
//                    case "Terradue.Tep.WebServer.Services.LoginServiceTep":
//                    case "Terradue.Tep.WebServer.Services.UserGithubServiceTep":
//                    case "Terradue.Tep.WebServer.Services.GroupServiceTep":
//                        log4net.GlobalContext.Properties["teplogtype"] = "User";
//                        break;
//                
//                    //OPENNEBULA
//                    case "Terradue.Tep.WebServer.Services.OneImageServiceTep":
//                    case "Terradue.Tep.WebServer.Services.OneServiceT ep":
//                    case "Terradue.Tep.WebServer.Services.OneUserServiceTep":
//                    case "Terradue.Tep.WebServer.Services.OneWpsServiceTep":
//                        log4net.GlobalContext.Properties["teplogtype"] = "One";
//                        break;
//
//                    //SERVICE
//                    case "Terradue.Tep.WebServer.Services.ProxyServiceTep":
//                    case "Terradue.Tep.WebServer.Services.ShareServiceTep":
//                    case "Terradue.Tep.WebServer.Services.WpsServiceTep":
//                    case "Terradue.Tep.WebServer.Services.WpsJobServiceTep":
//                        log4net.GlobalContext.Properties["teplogtype"] = "Service";
//                        break;
//
//                    //NEWS
//                    case "Terradue.Tep.WebServer.Services.NewsServiceTep":
//                    case "Terradue.Tep.WebServer.Services.DiscourseServiceTep":
//                    case "Terradue.Tep.WebServer.Services.RssNewsServiceTep":
//                    case "Terradue.Tep.WebServer.Services.TumblrNewsServiceTep":
//                    case "Terradue.Tep.WebServer.Services.TwitterNewsServiceTep":
//                        log4net.GlobalContext.Properties["teplogtype"] = "News";
//                        break;
//                
//                    //ACTIVITY
//                    case "Terradue.Tep.WebServer.Services.ActivityServiceTep":
//                    case "Terradue.Tep.WebServer.Services.LogServiceTep":
//                    case "Terradue.Tep.WebServer.Services.ReportServiceTep":
//                        log4net.GlobalContext.Properties["teplogtype"] = "Activity";
//                        break;
//
//                    default:
//                        log4net.GlobalContext.Properties["teplogtype"] = "N/A";
//                        break;
//                }
//            }catch(Exception){}
        }
    }
}

