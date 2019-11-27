using System;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;

namespace Terradue.Tep.WebServer.Services {

    [Route("/geoserver/layers/styles", "GET", Summary = "GET geoserver styles for layer", Notes = "")]
    public class GetGeoserverStylesRequestTep : IReturn<HttpResult> {
        [ApiMember(Name = "identifier", Description = "layer identifier", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string identifier { get; set; }
    }

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
             EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class GeoserverServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetGeoserverStylesRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            GeoserverStylesResponse result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/geoserver/layers/styles GET, identifier='{0}'", request.identifier));

                result = GeoserverFactory.GetStyles(request.identifier);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }

            return new HttpResult(result);
        }
    }
}
