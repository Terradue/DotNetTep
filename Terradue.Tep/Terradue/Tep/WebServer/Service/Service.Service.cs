using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure   | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ServiceServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        public object Get(ServiceServiceTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            context.Open();
            EntityList<Terradue.Portal.Service> services = new EntityList<Terradue.Portal.Service>(context);
            services.Load();

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            string format;
            if ( Request.QueryString["format"] == null ) format = "atom";
            else format = Request.QueryString["format"];

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest.QueryString, httpRequest.Headers, ose);
            IOpenSearchResultCollection osr = ose.Query(services, httpRequest.QueryString, responseType);

            context.Close ();

            return new HttpResult(osr.SerializeToString(), osr.ContentType);          
        }

        public object Post(BulkServicesForAppRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try
            {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/bulk/app POST, identifiers='{0}', app='{1}'", string.Join(",", request.Identifiers), request.SelfApp));
                if (request.Identifiers == null || request.Identifiers.Count == 0) return new HttpResult("No services specified", HttpStatusCode.BadRequest);
                if (request.SelfApp == null) return new HttpResult("No app specified", HttpStatusCode.BadRequest);

                Domain domain = null;
                string tags = null;

                var settings = MasterCatalogue.OpenSearchFactorySettings;
                var urlBOS = new UrlBasedOpenSearchable(context, new OpenSearchUrl(request.SelfApp), settings);
                var entity = urlBOS.Entity;
                if (entity is EntityList<ThematicApplicationCached>)
                {
                    var entitylist = entity as EntityList<ThematicApplicationCached>;
                    var items = entitylist.GetItemsAsList();
                    if (items.Count > 0)
                    {
                        var feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(items[0].TextFeed);
                        if (feed != null)
                        {
                            var entry = feed.Items.First();
                            var offering = entry.Offerings.First(p => p.Code == "http://www.opengis.net/spec/owc/1.0/req/atom/wps");
                            if (offering == null) return new HttpResult("No WPS offering in specified app", HttpStatusCode.BadRequest);

                            //get domain and tag for app
                            var op = offering.Operations.FirstOrDefault(o => o.Code == "ListProcess");
                            if (op != null && op.Href != null)
                            {
                                var nvc = HttpUtility.ParseQueryString((new Uri(op.Href)).Query);
                                foreach (var key in nvc.AllKeys)
                                {
                                    switch (key)
                                    {
                                        case "domain":                                            
                                            domain = Domain.FromIdentifier(context, nvc[key]);
                                            break;
                                        case "tag":
                                            tags = nvc[key];
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var identifier in request.Identifiers)
                {
                    try
                    {
                        var service = WpsProcessOfferingTep.FromIdentifier(context, identifier);
                        var newService = WpsProcessOfferingTep.Copy(service, context);
                        newService.Domain = domain;
                        newService.Tags = tags;
                        newService.Available = true;
                        newService.Store();
                    }
                    catch (Exception e)
                    {
                        context.LogError(this, "Error while loading service " + identifier, e);
                    }
                }
            }
            catch (Exception e)
            {
                context.LogError(this, e.Message, e);
                context.Close();
                return new HttpResult(e.Message, HttpStatusCode.BadRequest);
            }

            context.Close();

            return new HttpResult("", HttpStatusCode.OK);
        }

        public object Put(BulkServicesAvailabilityRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try
            {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/bulk/available POST, identifiers='{0}'", string.Join(",", request.Identifiers)));
                if (request.Identifiers == null || request.Identifiers.Count == 0) return new HttpResult("No services specified", HttpStatusCode.BadRequest);                

                
                foreach (var identifier in request.Identifiers)
                {
                    try
                    {
                        var service = WpsProcessOfferingTep.FromIdentifier(context, identifier);                        
                        service.Available = request.Available;
                        service.Store();
                    }
                    catch (Exception e)
                    {
                        context.LogError(this, "Error while loading service " + identifier, e);
                    }
                }
            }
            catch (Exception e)
            {
                context.LogError(this, e.Message, e);
                context.Close();
                return new HttpResult(e.Message, HttpStatusCode.BadRequest);
            }

            context.Close();

            return new HttpResult("", HttpStatusCode.OK);
        }

        public object Delete(BulkServicesDeleteRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try
            {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/bulk/delete DELETE, identifiers='{0}'", string.Join(",", request.Identifiers)));
                if (request.Identifiers == null || request.Identifiers.Count == 0) return new HttpResult("No services specified", HttpStatusCode.BadRequest);                

                
                foreach (var identifier in request.Identifiers)
                {
                    try
                    {
                        var service = WpsProcessOfferingTep.FromIdentifier(context, identifier);                        
                        service.Delete();
                    }
                    catch (Exception e)
                    {
                        context.LogError(this, "Error while loading service " + identifier, e);
                    }
                }
            }
            catch (Exception e)
            {
                context.LogError(this, e.Message, e);
                context.Close();
                return new HttpResult(e.Message, HttpStatusCode.BadRequest);
            }

            context.Close();

            return new HttpResult("", HttpStatusCode.OK);
        }

        public object Put(BulkSwitchServiceRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);

            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/bulk/replace PUT identifiers='{0}' newIdentifier='{1}'", string.Join(",", request.Identifiers), request.Service != null ? request.Service.Identifier : null));

                if (request.Identifiers == null || request.Identifiers.Count == 0) return new HttpResult("No services specified", HttpStatusCode.BadRequest);                
                if (request.Service == null) return new HttpResult("No service specified", HttpStatusCode.BadRequest);                
         
                foreach (var identifier in request.Identifiers)
                {
                    try
                    {
                        WpsProcessOfferingTep wpsOld = WpsProcessOfferingTep.FromIdentifier(context, identifier);   
                        WpsProcessOffering wpsNew = (WpsProcessOffering)request.Service.ToEntity(context, new WpsProcessOffering(context));
                        wpsNew.Identifier = Guid.NewGuid().ToString();
                        
                        if (!string.IsNullOrEmpty(request.WpsIdentifier)) {
                            var provider = (WpsProvider)ComputingResource.FromIdentifier(context, request.WpsIdentifier);
                            wpsNew.Provider = provider;
                        } else {
                            wpsNew.Provider = wpsOld.Provider;
                        }
                        wpsNew.DomainId = wpsOld.DomainId;
                        wpsNew.Tags = wpsOld.Tags;
                        wpsNew.IconUrl = wpsOld.IconUrl;
                        wpsNew.Available = true;
                        wpsNew.Commercial = wpsOld.Commercial;
                        wpsNew.Geometry = wpsOld.Geometry;
                        wpsNew.ValidationUrl = wpsOld.ValidationUrl;
                        wpsNew.TutorialUrl = wpsOld.TutorialUrl;
                        wpsNew.MediaUrl = wpsOld.MediaUrl;
                        wpsNew.SpecUrl = wpsOld.SpecUrl;
                        wpsNew.TermsConditionsUrl = wpsOld.TermsConditionsUrl;
                        wpsNew.TermsConditionsText = wpsOld.TermsConditionsText;
                        wpsNew.Store();

                        if (request.DeleteOld)
                            wpsOld.Delete();
                        else {
                            wpsOld.Available = false;
                            wpsOld.Store();
                        }
                    }
                    catch (Exception e)
                    {
                        context.LogError(this, "Error while loading service " + identifier, e);
                    }
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new HttpResult("", HttpStatusCode.OK);
        }

    }
}

