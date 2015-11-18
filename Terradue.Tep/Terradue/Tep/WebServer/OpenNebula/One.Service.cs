using System;
using System.Collections.Generic;
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
    public class OneService : ServiceStack.ServiceInterface.Service {

        public object Get(OneGetConfigRequestTep request) {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();

                string key = "One-access";
                result.Add(new KeyValuePair<string, string>(key,context.GetConfigValue(key)));

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }
    }

    [Route("/one/config", "GET", Summary = "GET opennebula config", Notes = "Get OpenNebula config")]
    public class OneGetConfigRequestTep : IReturn<List<KeyValuePair<string, string>>> {}
    
}