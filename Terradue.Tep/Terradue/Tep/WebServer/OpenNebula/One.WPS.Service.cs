using System;
using System.Collections.Generic;
using System.Xml;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using Terradue.OpenNebula;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class OneWpsServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(OneGetWPSRequestTep request) {
            List<WpsProvider> result = new List<WpsProvider>();

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/one/wps GET"));

                CloudWpsFactory wpsFinder = new CloudWpsFactory(context);
                result = wpsFinder.GetWPSFromVMs();

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }



    }

    [Route("/one/wps", "GET", Summary = "GET a list of WPS services for the user", Notes = "Get list of OpenNebula WPS")]
    public class OneGetWPSRequestTep : IReturn<List<WpsProcessOffering>> {}

}