using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Schema;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class RatesServiceTep : ServiceStack.ServiceInterface.Service {
        
        public object Post(AddRatesForServiceRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);

            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{serviceIdentifier}}/rates POST serviceIdentifier='{0}', Identifier='{1}', Unit='{2}', Cost='{3}'", request.ServiceIdentifier, request.Identifier, request.Unit, request.Cost));

                var service = WpsProcessOffering.FromIdentifier(context, request.ServiceIdentifier);
                Rates rate = request.ToEntity(context, service);
                rate.Store();

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Delete(DeleteRatesFromServiceRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);

            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{serviceIdentifier}}/rates DELETE serviceIdentifier='{0}', Identifier='{1}'", request.ServiceIdentifier, request.Identifier));

                var service = WpsProcessOffering.FromIdentifier(context, request.ServiceIdentifier);
                Rates rate = request.ToEntity(context, service);
                rate.Store();

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Get(RatesForServiceSearchRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            IOpenSearchResultCollection result;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{identifier}}/rates/search GET, identifier='{0}'", request.ServiceIdentifier));

                // Load the complete request
                var httpRequest = HttpContext.Current.Request;

                var service = WpsProcessOffering.FromIdentifier(context, request.ServiceIdentifier);

                EntityList<Rates> rates = new EntityList<Rates>(context);
                rates.SetFilter("EntityId",service.Id.ToString());
                rates.SetFilter("EntityTypeId",EntityType.GetEntityType(typeof(WpsProcessOffering)).Id.ToString());

                var ose = MasterCatalogue.OpenSearchEngine;

                Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
                result = ose.Query(rates, httpRequest.QueryString, responseType);

                OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(rates, result);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new HttpResult(result.SerializeToString(), result.ContentType);
        }

        public object Get(RatesForServiceDescriptionRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{identifier}}/rates/description GET, identifier='{0}'", request.ServiceIdentifier));

                EntityList<Rates> rates = new EntityList<Rates>(context);
                rates.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                var osd = rates.GetOpenSearchDescription();

                context.Close();

                return new HttpResult(osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
        }
               
    }
}
