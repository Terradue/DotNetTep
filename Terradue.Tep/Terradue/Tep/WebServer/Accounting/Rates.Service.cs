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
            WebRates result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{serviceIdentifier}}/rates POST serviceIdentifier='{0}', Identifier='{1}', Unit='{2}', Cost='{3}'", request.ServiceIdentifier, request.Identifier, request.Unit, request.Cost));

                var service = WpsProcessOffering.FromIdentifier(context, request.ServiceIdentifier);
                Rates rate = request.ToEntity(context, service);
                rate.Store();

                result = new WebRates(rate);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Delete(DeleteRatesFromServiceRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);

            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{serviceIdentifier}}/rates/{{id}} DELETE serviceIdentifier='{0}', Id='{1}'", request.ServiceIdentifier, request.Id));

                var service = Service.FromIdentifier(context, request.ServiceIdentifier);
                Rates rate = Rates.FromId(context, request.Id);
                context.LogInfo(this, string.Format("Deleting rates {0} of service {1}", rate.Identifier, service.Identifier));
                rate.Delete();

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Get(RatesForServiceRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            List<WebRates> result = new List<WebRates>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{identifier}}/rates GET, identifier='{0}'", request.ServiceIdentifier));

                // Load the complete request
                var httpRequest = HttpContext.Current.Request;

                var service = WpsProcessOffering.FromIdentifier(context, request.ServiceIdentifier);

                EntityList<Rates> rates = new EntityList<Rates>(context);
                rates.SetFilter("EntityId",service.Id.ToString());
                rates.SetFilter("EntityTypeId",EntityType.GetEntityType(typeof(WpsProcessOffering)).Id.ToString());
                rates.Load();

                foreach(var rate in rates.GetItemsAsList()){
                    result.Add(new WebRates(rate));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }
 
    }
}
