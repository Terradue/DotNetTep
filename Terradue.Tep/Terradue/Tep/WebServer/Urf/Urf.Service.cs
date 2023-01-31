using ServiceStack.ServiceHost;
using Terradue.Tep.WebServer;
using System;
using Terradue.Portal;
using Terradue.WebService.Model;
using System.IO;
using ServiceStack.Text;
using System.Net;
using System.Web;
using ServiceStack.Common.Web;
using Terradue.Portal.Urf;
using System.Collections.Generic;
using System.Linq;

namespace Terradue.Tep.WebServer.Services {

    [Route("/user/current/urf", "GET", Summary = "create URF", Notes = "")]
    public class GetCurrentUserURFsRequestTep {}

    [Route("/user/{id}/urf", "GET", Summary = "create URF", Notes = "")]
    public class GetUserURFsRequestTep {
        [ApiMember(Name = "id", Description = "user id", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string id { get; set; }
    }

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    [Api("Tep library")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    /// <summary>
    /// Login service. Used to log into the system (replacing UMSSO for testing)
    /// </summary>
    public class UrfServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetCurrentUserURFsRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            List<List<UrfTep>> urfs = null;
            try {
                context.LogInfo(this, string.Format("/user/current/urf GET"));
                context.Open();

                var usr = UserTep.FromId(context, context.UserId);
                if (string.IsNullOrEmpty(usr.TerradueCloudUsername)) usr.LoadCloudUsername();
                if (string.IsNullOrEmpty(usr.TerradueCloudUsername)) throw new Exception("Impossible to get Terradue username");

                urfs = usr.LoadASDs();
                
            } catch (Exception e) {
                context.LogError(this, e.Message);
                throw e;
            }

            context.Close();
            return new HttpResult(urfs);
        }

        public object Get(GetUserURFsRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            List<List<UrfTep>> urfs = null;
            try {
                context.LogInfo(this, string.Format("/user/{0}/urf GET", request.id));
                context.Open();

                var usr = UserTep.FromIdentifier(context, request.id);
                urfs = usr.LoadASDs();                

            } catch (Exception e) {
                context.LogError(this, e.Message);
                throw e;
            }

            context.Close();
            return new HttpResult(urfs);
        }

    }

}
