using System;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;

namespace Terradue.Tep.WebServer.Services {

    [Route("/geoserver/layers/styles", "GET", Summary = "GET geoserver styles for layer", Notes = "")]
    public class GetGeoserverLayerStylesRequestTep : IReturn<HttpResult> {
        [ApiMember(Name = "identifier", Description = "layer identifier", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string identifier { get; set; }
    }

    [Route("/geoserver/workspace/styles", "GET", Summary = "GET geoserver styles for workspace", Notes = "")]
    public class GetGeoserverWorkspaceStylesRequestTep : IReturn<HttpResult> {
        [ApiMember(Name = "identifier", Description = "layer identifier", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string identifier { get; set; }
    }

    [Route("/geoserver/styles", "GET", Summary = "GET geoserver global styles", Notes = "")]
    public class GetGeoserverGlobalStylesRequestTep : IReturn<HttpResult> {
    }

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
             EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class GeoserverServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetGeoserverGlobalStylesRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            GeoserverStylesResponse result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/geoserver/styles GET"));

                result = GeoserverFactory.GetGlobalStyles();

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }

            return new HttpResult(result);
        }

        public object Get(GetGeoserverLayerStylesRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            GeoserverStylesResponse result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/geoserver/layers/styles GET, identifier='{0}'", request.identifier));

                result = GeoserverFactory.GetStylesForLayer(request.identifier);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }

            return new HttpResult(result);
        }

        public object Get(GetGeoserverWorkspaceStylesRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            GeoserverStylesResponse result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/geoserver/workspace/styles GET, identifier='{0}'", request.identifier));

                result = GeoserverFactory.GetStylesForWorkspace(request.identifier);

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
