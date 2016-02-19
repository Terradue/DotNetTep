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

        public object Get(OneGetWPSRequestTep request) {
            List<WpsProvider> result = new List<WpsProvider>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();

                CloudWpsFactory wpsFinder = new CloudWpsFactory(context);
                result = wpsFinder.GetWPSFromVMs();

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }



    }

    [Route("/one/wps", "GET", Summary = "GET a list of WPS services for the user", Notes = "Get list of OpenNebula WPS")]
    public class OneGetWPSRequestTep : IReturn<List<WpsProcessOffering>> {}

    [Route("/one/user/{id}", "POST", Summary = "GET a user of opennebula", Notes = "Get OpenNebula user")]
    public class OneCreateWPSProcessRequestTep : IReturn<List<string>> {
        [ApiMember(Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }
}