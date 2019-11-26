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
        
        public object Post(AddRatesForProviderRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebRates result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/cr/wps/{{providerIdentifier}}/rates POST providerIdentifier='{0}', Identifier='{1}', Unit='{2}', Cost='{3}'", request.ProviderIdentifier, request.Identifier, request.Unit, request.Cost));

                var service = WpsProvider.FromIdentifier(context, request.ProviderIdentifier);
                Rates rate = request.ToEntity(context, service);
                rate.Store();

                result = new WebRates(rate);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Delete(DeleteRatesFromProviderRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);

            try {
                context.Open();
                context.LogInfo(this, string.Format("/cr/wps/{{providerIdentifier}}/rates/{{id}} DELETE providerIdentifier='{0}', Id='{1}'", request.ProviderIdentifier, request.Id));

                var service = WpsProvider.FromIdentifier(context, request.ProviderIdentifier);
                Rates rate = Rates.FromId(context, request.Id);
                context.LogInfo(this, string.Format("Deleting rates {0} of service {1}", rate.Identifier, service.Identifier));
                rate.Delete();

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Get(RatesForProviderRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            List<WebRates> result = new List<WebRates>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/cr/wps/{{identifier}}/rates GET, identifier='{0}'", request.ProviderIdentifier));

                // Load the complete request
                var httpRequest = HttpContext.Current.Request;

                var service = WpsProvider.FromIdentifier(context, request.ProviderIdentifier);

                EntityList<Rates> rates = new EntityList<Rates>(context);
                rates.SetFilter("EntityId",service.Id.ToString());
                rates.SetFilter("EntityTypeId",EntityType.GetEntityType(typeof(WpsProvider)).Id.ToString());
                rates.Load();

                foreach(var rate in rates.GetItemsAsList()){
                    result.Add(new WebRates(rate));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }
 
    }
}
