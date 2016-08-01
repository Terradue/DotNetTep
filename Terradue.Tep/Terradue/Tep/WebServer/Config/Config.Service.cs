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
    public class ConfigServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetConfig request) {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();

                result.Add(new KeyValuePair<string, string>("geobrowserDateMin",context.GetConfigValue("geobrowser-date-min")));
                result.Add(new KeyValuePair<string, string>("geobrowserDateMax",context.GetConfigValue("geobrowser-date-max")));
                result.Add(new KeyValuePair<string, string>("Github-client-id",context.GetConfigValue("Github-client-id")));
                    
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }
    }
}