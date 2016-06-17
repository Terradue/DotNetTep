using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml;
using OpenGis.Wps;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

     [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json | EndpointAttributes.Xml,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json | EndpointAttributes.Xml)]
    public class WpsServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(SearchWPSProviders request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            context.RestrictedMode = false;
            object result;
            context.Open();

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            EntityList<WpsProvider> wpsProviders = new EntityList<WpsProvider>(context);
            wpsProviders.Load();
            wpsProviders.OpenSearchEngine = ose;

            CloudWpsFactory wpsFinder = new CloudWpsFactory(context);
            foreach (WpsProvider wps in wpsFinder.GetWPSFromVMs())
                wpsProviders.Include(wps);

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;

            string format;
            if (Request.QueryString["format"] == null)
                format = "atom";
            else
                format = Request.QueryString["format"];

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query(wpsProviders, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(wpsProviders, osr);
            //            OpenSearchFactory.ReplaceSelfLinks(wpsProviders, httpRequest.QueryString, osr.Result, EntrySelfLinkTemplate );

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }

        public object Get(SearchWPSServices request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            object result;
            context.Open();

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            EntityList<WpsProcessOffering> wpsProcesses = new EntityList<WpsProcessOffering>(context);
            wpsProcesses.Template.Available = true;
            wpsProcesses.Load();
            wpsProcesses.OpenSearchEngine = ose;

            //+ WPS from cloud
            List<WpsProvider> wpss = new List<WpsProvider>();
            try {
                CloudWpsFactory wpsFinder = new CloudWpsFactory(context);
                wpss = wpsFinder.GetWPSFromVMs();
            } catch (Exception e) {
                //we do nothing, we will return the list without any WPS from the cloud
            }
            foreach (WpsProvider wps in wpss) {
                try {
                    foreach (WpsProcessOffering process in wps.GetWpsProcessOfferingsFromUrl(wps.BaseUrl)) {
                        wpsProcesses.Include(process);
                    }
                } catch (Exception e) {
                    //we do nothing, we just dont add the process
                }
            }

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;

            string format;
            if (Request.QueryString["format"] == null)
                format = "atom";
            else
                format = Request.QueryString["format"];

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query(wpsProcesses, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(wpsProcesses, osr);
            //            OpenSearchFactory.ReplaceSelfLinks(wpsProcesses, httpRequest.QueryString, osr.Result, EntrySelfLinkTemplate );

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }

        public object Get(GetWPSServices request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            List<WebServiceTep> result = new List<WebServiceTep>();
            try {
                context.Open();

                EntityList<WpsProcessOffering> services = new EntityList<WpsProcessOffering>(context);
                services.Load();

                //+ WPS from cloud
                List<WpsProvider> wpss = new List<WpsProvider>();
                try {
                    CloudWpsFactory wpsFinder = new CloudWpsFactory(context);
                    wpss = wpsFinder.GetWPSFromVMs();
                } catch (Exception e) {
                    //we do nothing, we will return the list without any WPS from the cloud
                }
                foreach (WpsProvider wps in wpss) {
                    try {
                        foreach (WpsProcessOffering process in wps.GetWpsProcessOfferingsFromUrl(wps.BaseUrl)) {
                            process.UserId = 0;
                            services.Include(process);
                        }
                    } catch (Exception e) {
                        //we do nothing, we just dont add the process
                    }
                }

                int maxid =1;
                foreach (WpsProcessOffering wps in services) {
                    WebServiceTep wpsresult = new WebServiceTep(context, wps);
                    wpsresult.Provider = wps.Provider.Identifier;
                    if(wps.Id == 0) 
                        wpsresult.Id = ++maxid;
                    else
                        maxid = Math.Max(maxid,wps.Id);
                    result.Add(wpsresult);
                }

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }

            return result;
        }

        public object Get(GetWebProcessingServices request) {
            IfyWebContext context;
            System.IO.Stream stream = new System.IO.MemoryStream();
            context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            context.RestrictedMode = false;

            context.Open();

            if (request.Service.ToLower() != "wps")
                throw new Exception("Web Processing Service Request is not valid");
            WpsProcessOffering wps = null;
            switch (request.Request.ToLower()) {
                case "getcapabilities":
                    log.Info("WPS GetCapabilities requested");
                    try{
                        WpsFactory factory = new WpsFactory(context);
                        var getCapabilities = factory.WpsGetCapabilities();

                        context.Close();
                        System.Xml.Serialization.XmlSerializerNamespaces ns = new System.Xml.Serialization.XmlSerializerNamespaces();
                        ns.Add("wps", "http://www.opengis.net/wps/1.0.0");
                        ns.Add("ows", "http://www.opengis.net/ows/1.1");
                        ns.Add("xlink", "http://www.w3.org/1999/xlink");

                        new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.WPSCapabilitiesType)).Serialize(stream, getCapabilities, ns);
                        return new HttpResult(stream, "application/xml");
                    }catch(Exception e){
                        return new HttpError(HttpStatusCode.BadRequest, e);
                    }

                case "describeprocess":
                    log.Info("WPS DescribeProcess requested");
                    try{
                        wps = CloudWpsFactory.GetWpsProcessOffering(context, request.Identifier);
                        var describeResponse = wps.DescribeProcess();

                        if (describeResponse is ProcessDescriptions) {
                            var descResponse = describeResponse as ProcessDescriptions;
                            descResponse = WpsFactory.DescribeProcessCleanup(descResponse);
                            System.Xml.Serialization.XmlSerializerNamespaces ns = new System.Xml.Serialization.XmlSerializerNamespaces();
                            ns.Add("wps", "http://www.opengis.net/wps/1.0.0");
                            new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ProcessDescriptions)).Serialize(stream, descResponse, ns);    
                            return new HttpResult(stream, "application/xml");
                        } else {
                            return new HttpError("Unknown error during the DescribeProcess", HttpStatusCode.BadRequest, "NoApplicableCode", "");
                        }
                    }catch(Exception e){
                        return new HttpError(HttpStatusCode.BadRequest, e.Message);
                    }

                case "execute":
                    log.Info("WPS Execute requested");
                    Execute executeInput = new Execute();

                    executeInput.Identifier =  new OpenGis.Wps.CodeType{ Value = request.Identifier};
                    executeInput.service = request.Service;
                    executeInput.version = request.Version;
                    executeInput.DataInputs = new List<OpenGis.Wps.InputType>();
                    foreach (var param in request.DataInputs.Split(";".ToCharArray())) {
                        var key = param.Substring(0,param.IndexOf("="));
                        var value = param.Substring(param.IndexOf("=") + 1);
                        OpenGis.Wps.InputType input = new OpenGis.Wps.InputType();
                        input.Identifier = new OpenGis.Wps.CodeType{ Value = key };
                        input.Data = new OpenGis.Wps.DataType{ Item = new OpenGis.Wps.LiteralDataType{ Value = value } };
                        executeInput.DataInputs.Add(input);
                    }

                    var response = Execute(context, executeInput);
                    context.Close();
                    return response;
                default:
                    context.Close();
                    throw new Exception("Web Processing Service Request is not valid");
            }

        }

        public object Post(WpsExecutePostRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            context.Open();
            log.Info("WPS Execute requested (POST)");

            Execute executeInput = (Execute)new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.Execute)).Deserialize(request.RequestStream);
            log.Debug("Deserialization done");
            var response = Execute(context, executeInput);
            context.Close();
            return response;
        }

        private object Execute(IfyContext context, Execute executeInput){
            WpsProcessOffering wps = CloudWpsFactory.GetWpsProcessOffering(context, executeInput.Identifier.Value);
            object executeResponse = null;
            var stream = new System.IO.MemoryStream();

            try{
                executeResponse = wps.Execute(executeInput);

                if (executeResponse is ExecuteResponse) {
                    log.Debug("Execute response ok");
                    var execResponse = executeResponse as ExecuteResponse;

                    log.Debug("Creating job");
                    WpsJob wpsjob = null;
                    try{
                        wpsjob = CreateJobFromExecute(context, wps, execResponse, executeInput);
                    }catch(Exception e){
                        log.Error(e.Message);
                        throw e;
                    }
                    log.Debug("job created");

                    Uri uri = new Uri(execResponse.serviceInstance);
                    execResponse.serviceInstance = context.BaseUrl + uri.PathAndQuery;
                    execResponse.statusLocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + wpsjob.Identifier;
                    new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExecuteResponse)).Serialize(stream, execResponse);

                    return new HttpResult(stream, "application/xml");
                } else if (executeResponse is ExceptionReport) {
                    log.Debug("Exception report ok");
                    var exceptionReport = executeResponse as ExceptionReport;
                    new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExceptionReport)).Serialize(stream, exceptionReport);
                    stream.Seek(0, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(stream)) {
                        string errormsg = reader.ReadToEnd();
                        log.Error(errormsg);
                        return new HttpError(errormsg, HttpStatusCode.BadRequest, exceptionReport.Exception[0].exceptionCode, exceptionReport.Exception[0].ExceptionText[0]);
                    }
                } else {
                    return new HttpError("Unknown error during the Execute", HttpStatusCode.BadRequest, "NoApplicableCode", "");
                }
            }catch(Exception e){
                return new HttpError(e.Message);
            }
        }

        private WpsJob CreateJobFromExecute(IfyContext context, WpsProcessOffering wps, ExecuteResponse execResponse, Execute executeInput){
            Uri uri = new Uri(execResponse.statusLocation);
            string newId = Guid.NewGuid().ToString();

            //create WpsJob
            log.Debug("Get identifier from status location");
            string identifier = null;
            try {
                identifier = uri.Query.Substring(uri.Query.IndexOf("id=") + 3);
            } catch (Exception e) {
                log.Debug("identifier does not contain id=");
                //statusLocation url is different for gpod
                if (uri.AbsoluteUri.Contains("gpod.eo.esa.int")) {
                    log.Debug("identifier taken from gpod url : " + uri.AbsoluteUri);
                    identifier = uri.AbsoluteUri.Substring(uri.AbsoluteUri.LastIndexOf("status") + 7);
                } else if (uri.AbsoluteUri.Contains("pywps")) {
                    identifier = uri.AbsoluteUri;
                    identifier = identifier.Substring(identifier.LastIndexOf("pywps-") + 7);
                    identifier = identifier.Substring(0, identifier.LastIndexOf(".xml"));
                } else {
                    log.Error(e.Message);
                    throw e;
                }
            }
            log.Debug("identifier = " + identifier);
            log.Debug("Provider is null ? -> " + (wps.Provider == null ? "true" : "false"));
            WpsJob wpsjob = new WpsJob(context);
            wpsjob.Name = wps.Name;
            wpsjob.RemoteIdentifier = identifier;
            wpsjob.Identifier = newId;
            wpsjob.OwnerId = context.UserId;
            wpsjob.UserId = context.UserId;
            wpsjob.WpsId = wps.Provider.Identifier;
            wpsjob.ProcessId = wps.Identifier;
            wpsjob.CreatedTime = DateTime.Now;

            //in case of username:password in the provider url, we take them from provider
            var statusuri = new UriBuilder(wps.Provider.BaseUrl);
            var statusuri2 = new UriBuilder(execResponse.statusLocation);
            statusuri2.UserName = statusuri.UserName;
            statusuri2.Password = statusuri.Password;
            wpsjob.StatusLocation = statusuri2.Uri.AbsoluteUri;

            wpsjob.Parameters = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> output = new List<KeyValuePair<string, string>>();
            foreach (var d in executeInput.DataInputs) {
                log.Debug("Input: " + d.Identifier.Value);
                if (d.Data != null && d.Data.Item != null) {
                    if (d.Data.Item is LiteralDataType) {
                        log.Debug("Value is LiteralDataType");
                        output.Add(new KeyValuePair<string, string>(d.Identifier.Value, ((LiteralDataType)(d.Data.Item)).Value));  
                    } else if (d.Data.Item is ComplexDataType) {
                        log.Debug("Value is ComplexDataType");
                        throw new Exception("Data Input ComplexDataType not yet implemented");
                    } else if (d.Data.Item is BoundingBoxType) {
                        //for BoundingBoxType, webportal creates LowerCorner and UpperCorner
                        //we just need to save both values as a concatained string
                        log.Debug("Value is BoundingBoxType");
                        var bbox = d.Data.Item as BoundingBoxType;
                        var bboxVal = (bbox != null && bbox.UpperCorner != null && bbox.LowerCorner != null) ? bbox.LowerCorner.Replace(" ", ",") + "," + bbox.UpperCorner.Replace(" ", ",") : "";
                        output.Add(new KeyValuePair<string, string>(d.Identifier.Value, bboxVal));  
                    } else {
                        throw new Exception("unhandled type of Data");
                    } 
                } else if (d.Reference != null){
                    log.Debug("Value is InputReferenceType");
                    if (!string.IsNullOrEmpty(d.Reference.href)) {
                        output.Add(new KeyValuePair<string, string>(d.Identifier.Value, d.Reference.href));
                    } else if (d.Reference.Item != null){
                        throw new Exception("Data Input InputReferenceType item not yet implemented");
                    }
                }
            }
            wpsjob.Parameters = output;
            wpsjob.Store();

            return wpsjob;
        }

        public object Get(GetResultsServlets request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.RestrictedMode = false;
            System.IO.Stream stream = new System.IO.MemoryStream();
            try{
                context.Open();
                //load job from request identifier
                WpsJob wpsjob = WpsJob.FromIdentifier(context, request.Id);
                log.Info(string.Format("Get Job {0} status info",wpsjob.Identifier));
                string executeUrl = wpsjob.StatusLocation;

                HttpWebRequest executeHttpRequest = WpsProvider.CreateWebRequest(executeUrl);
                    //(HttpWebRequest)WebRequest.Create(executeUrl);
                if (executeUrl.Contains("gpod.eo.esa.int")) {
                    executeHttpRequest.Headers.Add("X-UserID", context.GetConfigValue("GpodWpsUser"));  
                }

                var remoteWpsResponseStream = new MemoryStream();
                HttpWebResponse remoteWpsResponse = null;

                log.Debug(string.Format("Status url = {0}",executeHttpRequest.RequestUri.AbsoluteUri));

                try {
                    remoteWpsResponse = (HttpWebResponse)executeHttpRequest.GetResponse();
                } catch (WebException e) {
                    //PATCH, waiting for http://project.terradue.com/issues/13615 to be resolved
                    if (executeUrl.Contains("gpod.eo.esa.int")) {
                        remoteWpsResponse = (HttpWebResponse)e.Response;
                    } else if (e.Response != null) remoteWpsResponse = (HttpWebResponse)e.Response;
                    else {
                        log.Error(e.Message);
                        return new HttpResult(e.Message, HttpStatusCode.BadGateway);
                    }
                }

                try {
                    remoteWpsResponse.GetResponseStream().CopyTo(remoteWpsResponseStream);
                } catch (WebException e) {

                    // TODO: Remove this patch when Frank will correct GPOD
                    if (executeUrl.Contains("gpod.eo.esa.int")) {
                        e.Response.GetResponseStream().CopyTo(remoteWpsResponseStream);
                        if (remoteWpsResponseStream == null)
                            throw e;
                    } else {
                        log.Error(e.Message);
                        return new HttpResult(e.Message, HttpStatusCode.BadRequest);
                    }
                }

                OpenGis.Wps.ExecuteResponse execResponse = null; 
                try{
                    remoteWpsResponseStream.Seek(0, SeekOrigin.Begin);
                    execResponse = (OpenGis.Wps.ExecuteResponse)new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExecuteResponse)).Deserialize(remoteWpsResponseStream);
                }catch(Exception e){
                    remoteWpsResponseStream.Seek(0, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(remoteWpsResponseStream)) {
                        string errormsg = reader.ReadToEnd();
                        log.Error(errormsg);
                        return new HttpResult(errormsg, HttpStatusCode.BadRequest);
                    }
                }
                if(string.IsNullOrEmpty(execResponse.statusLocation)) execResponse.statusLocation = wpsjob.StatusLocation;
                Uri uri = new Uri(execResponse.statusLocation);
                string identifier;
                try {
                    identifier = uri.Query.Substring(uri.Query.IndexOf("id=") + 3);
                } catch (Exception e) {
                    if (executeUrl.Contains("gpod.eo.esa.int")) {
                        identifier = uri.AbsoluteUri.Substring(uri.AbsoluteUri.LastIndexOf("status") + 7);
                    } else {
                        log.Error(e.Message);
                        return new HttpResult(e.Message, HttpStatusCode.BadRequest);
                    }
                }

                execResponse.statusLocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + wpsjob.Identifier;

                if (execResponse.ProcessOutputs != null) {
                    foreach (OutputDataType output in execResponse.ProcessOutputs) {
                        try{
                            if(output.Item != null && output.Item is DataType && ((DataType)(output.Item)).Item != null) {
                                var item = ((DataType)(output.Item)).Item as ComplexDataType;
                                if (item.Reference != null && output.Identifier.Value.Equals("result_osd")) {
                                    var reference = item.Reference as OutputReferenceType;
                                    reference.href = context.BaseUrl + "/proxy?url=" + HttpUtility.UrlEncode(reference.href);
                                    item.Reference = reference;
                                    ((DataType)(output.Item)).Item = item;
                                } else if (item.Any != null) {
                                    var reference = new OutputReferenceType();
                                    reference.mimeType = "application/opensearchdescription+xml";
                                    reference.href = context.BaseUrl + "/proxy/gpod/" + wpsjob.Identifier + "/description";
                                    item.Reference = reference;
                                    item.Any = null;
                                    item.mimeType = "application/xml";
                                    output.Identifier = new CodeType{ Value = "result_osd" };
                                }
                            }
                        }catch(Exception){}
                    }
                }
                uri = new Uri(execResponse.serviceInstance);
                execResponse.serviceInstance = context.BaseUrl + uri.PathAndQuery;
                new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExecuteResponse)).Serialize(stream, execResponse);
                context.Close();

                return new HttpResult(stream, "application/xml");
            }catch(Exception e){
                return new HttpResult(e.Message, HttpStatusCode.BadRequest);
            }
        }

        public object Get(GetWPSProviders request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            context.RestrictedMode = false;
            List<WebWpsProvider> result = new List<WebWpsProvider>();
            try {
                context.Open();

                EntityList<WpsProvider> wpsProviders = new EntityList<WpsProvider>(context);
                wpsProviders.Load();

                foreach (WpsProvider wps in wpsProviders) {
                    result.Add(new WebWpsProvider(wps));
                }

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }

            return result;
        }

        public object Post(CreateWPSProvider request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebWpsProvider result = null;
            try {
                context.Open();

                WpsProvider wpsProvider = null;
                bool exists = false;
                try {
                    wpsProvider = (WpsProvider)WpsProvider.FromIdentifier(context, request.Identifier);
                    exists = true;
                } catch (Exception) {
                    exists = false;
                }

                if (exists)
                    throw new Exception("WPS already exists");

                //Create WPS provider
                wpsProvider = request.ToEntity(context, wpsProvider);
                wpsProvider.Store();

                //Make it public, the authorizations will then be done on the services
                wpsProvider.StoreGlobalPrivileges();

                wpsProvider.StoreProcessOfferings();

                result = new WebWpsProvider(wpsProvider);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Post(CreateWPSService request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebWpsService result = null;
            try {
                context.Open();

                WpsProcessOffering wps = null;
                bool exists = false;
                try {
                    wps = (WpsProcessOffering)WpsProcessOffering.FromIdentifier(context, request.Identifier);
                    exists = true;
                } catch (Exception) {
                    exists = false;
                }

                if (exists)
                    throw new Exception("WPS already exists");

                //Create WPS provider
                WpsProvider wpsProvider = new WpsProvider(context);
                wpsProvider.BaseUrl = request.Url;
                wpsProvider.Identifier = request.Identifier;
                wpsProvider.Name = request.Name;
                wpsProvider.Store();

                //Create WPS service
                wps = new WpsProcessOffering(context);
                wps.Identifier = request.Identifier;
                wps.Name = request.Name;
                wps.Description = request.Description;
                wps.Url = request.Url;
                wps.Provider = wpsProvider;
                wps.Store();

                result = new WebWpsService(wps);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Delete(DeleteWPSService request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            bool result = false;
            try {
                context.Open();

                WpsProcessOffering wps = null;
                bool exists = false;
                try {
                    wps = (WpsProcessOffering)WpsProcessOffering.FromId(context, request.Id);
                    wps.Delete();
                } catch (Exception e) {
                    throw e;
                }

                result = true;

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(result);
        }

        public object Delete(DeleteWPSProvider request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            bool result = false;
            try {
                context.Open();

                WpsProvider wps = null;
                bool exists = false;
                try {
                    wps = (WpsProvider)WpsProvider.FromId(context, request.Id);
                    wps.Delete();
                } catch (Exception e) {
                    throw e;
                }

                result = true;

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(result);
        }

        public object Put(UpdateWPSProvider request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebWpsProvider result = null;
            try {
                context.Open();

                WpsProvider wps = (request.Id == 0 ? null : (WpsProvider)WpsProvider.FromId(context, request.Id));

                if (!string.IsNullOrEmpty(request.Mode) && request.Mode.Equals("synchro"))
                    wps.UpdateProcessOfferings();
                else {
                    wps = request.ToEntity(context, wps);
                    wps.Store();
                }

                result = new WebWpsProvider(wps);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(WpsServiceUpdateRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebServiceTep result = null;
            try {
                context.Open();

                WpsProcessOffering wps = (request.Id == 0 ? null : (WpsProcessOffering)WpsProcessOffering.FromId(context, request.Id));

                if(request.Access != null){
                    switch(request.Access){
                        case "public":
                            wps.StoreGlobalPrivileges();
                            break;
                        case "private":
                            wps.RemoveGlobalPrivileges();
                            break;
                        default:
                            break;
                    }
                } else {
                    wps = (WpsProcessOffering)request.ToEntity(context, wps);
                    wps.Store();
                }

                result = new WebServiceTep(context, wps);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        private static OpenGis.Wps.WPSCapabilitiesType CreateGetCapabilititesTemplate(string baseUrl) {
            OpenGis.Wps.WPSCapabilitiesType capabilitites = new OpenGis.Wps.WPSCapabilitiesType();

            capabilitites.ServiceIdentification = new OpenGis.Wps.ServiceIdentification();
            capabilitites.ServiceIdentification.Title = new List<OpenGis.Wps.LanguageStringType>{ new OpenGis.Wps.LanguageStringType{ Value = "Geohazard Tep WPS" } };
            capabilitites.ServiceIdentification.Abstract = new List<OpenGis.Wps.LanguageStringType>{ new OpenGis.Wps.LanguageStringType{ Value = "Proxy WPS of the geohazard platform" } };
            capabilitites.ServiceIdentification.Keywords = new List<OpenGis.Wps.KeywordsType>();
            OpenGis.Wps.KeywordsType kw1 = new OpenGis.Wps.KeywordsType{ Keyword = new List<OpenGis.Wps.LanguageStringType>{ new OpenGis.Wps.LanguageStringType{ Value = "WPS" } } };
            List<OpenGis.Wps.LanguageStringType> listKeywords = new List<OpenGis.Wps.LanguageStringType>();
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "WPS" });
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "geohazards" });
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "geospatial" });
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "geoprocessing" });
            capabilitites.ServiceIdentification.Keywords.Add(new OpenGis.Wps.KeywordsType{ Keyword = listKeywords });
            capabilitites.ServiceIdentification.ServiceType = new OpenGis.Wps.CodeType{ Value = "WPS" };
            capabilitites.ServiceIdentification.ServiceTypeVersion = new List<string>{ "1.0.0" };
            capabilitites.ServiceIdentification.Fees = "None";
            capabilitites.ServiceIdentification.AccessConstraints = new List<string>{ "NONE" };

            capabilitites.ServiceProvider = new OpenGis.Wps.ServiceProvider();
            capabilitites.ServiceProvider.ProviderName = "Geohazards Tep";
            capabilitites.ServiceProvider.ProviderSite = new OpenGis.Wps.OnlineResourceType{ href = "https://geohazards-tep.eo.esa.int/" };

            capabilitites.OperationsMetadata = new OpenGis.Wps.OperationsMetadata();
            capabilitites.OperationsMetadata.Operation = new List<OpenGis.Wps.Operation>();
            //Add GetCapabilities OperationMetadata
            OpenGis.Wps.Operation getCapabilitiesOperation = new OpenGis.Wps.Operation();
            getCapabilitiesOperation.name = "GetCapabilities";
            getCapabilitiesOperation.DCP = new List<OpenGis.Wps.DCP>();
            OpenGis.Wps.DCP dcpGetCap = new OpenGis.Wps.DCP();
            dcpGetCap.Item = new OpenGis.Wps.HTTP();
            dcpGetCap.Item.Items = new List<OpenGis.Wps.RequestMethodType>();
            OpenGis.Wps.RequestMethodType getGetCapaDcp = new OpenGis.Wps.GetRequestMethodType();
            getGetCapaDcp.href = baseUrl;
            dcpGetCap.Item.Items.Add(getGetCapaDcp);
            getCapabilitiesOperation.DCP.Add(dcpGetCap);
            capabilitites.OperationsMetadata.Operation.Add(getCapabilitiesOperation);
            //Add DescribeProcess OperationMetadata
            OpenGis.Wps.Operation describeProcessOperation = new OpenGis.Wps.Operation();
            describeProcessOperation.name = "DescribeProcess";
            describeProcessOperation.DCP = new List<OpenGis.Wps.DCP>();
            OpenGis.Wps.DCP dcpDescrPr = new OpenGis.Wps.DCP();
            dcpDescrPr.Item = new OpenGis.Wps.HTTP();
            dcpDescrPr.Item.Items = new List<OpenGis.Wps.RequestMethodType>();
            OpenGis.Wps.RequestMethodType getDescrPrDcp = new OpenGis.Wps.GetRequestMethodType();
            getDescrPrDcp.href = baseUrl;
            dcpDescrPr.Item.Items.Add(getDescrPrDcp);
            describeProcessOperation.DCP.Add(dcpDescrPr);
            capabilitites.OperationsMetadata.Operation.Add(describeProcessOperation);
            //Add Execute OperationMetadata
            OpenGis.Wps.Operation executeOperation = new OpenGis.Wps.Operation();
            executeOperation.name = "Execute";
            executeOperation.DCP = new List<OpenGis.Wps.DCP>();
            OpenGis.Wps.DCP dcpExec = new OpenGis.Wps.DCP();
            dcpExec.Item = new OpenGis.Wps.HTTP();
            dcpExec.Item.Items = new List<OpenGis.Wps.RequestMethodType>();
            OpenGis.Wps.RequestMethodType getExecDcp = new OpenGis.Wps.GetRequestMethodType();
            getExecDcp.href = baseUrl;
            dcpExec.Item.Items.Add(getExecDcp);
            OpenGis.Wps.RequestMethodType postExecDcp = new OpenGis.Wps.PostRequestMethodType();
            postExecDcp.href = baseUrl;
            dcpExec.Item.Items.Add(postExecDcp);
            executeOperation.DCP.Add(dcpExec);
            capabilitites.OperationsMetadata.Operation.Add(executeOperation);

            capabilitites.ProcessOfferings = new OpenGis.Wps.ProcessOfferings();
            capabilitites.ProcessOfferings.Process = new List<OpenGis.Wps.ProcessBriefType>();

            capabilitites.Languages = new OpenGis.Wps.Languages();
            capabilitites.Languages.Default = new OpenGis.Wps.LanguagesDefault{ Language = "en-US" };

            return capabilitites;
        }

        public static string EntrySelfLinkTemplate(IOpenSearchResultItem item, OpenSearchDescription osd, string mimeType) {
            if (item == null)
                return null;

            NameValueCollection nvc = new NameValueCollection();

            nvc.Set("id", string.Format("{0}", item.Identifier));

            UriBuilder template = new UriBuilder(OpenSearchFactory.GetOpenSearchUrlByType(osd, mimeType).Template);
            string[] queryString = Array.ConvertAll(nvc.AllKeys, key => string.Format("{0}={1}", key, nvc[key]));
            template.Query = string.Join("&", queryString);
            return template.ToString();
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(WPSServiceGetGroupsRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();

                WpsProcessOffering wps = (WpsProcessOffering)WpsProcessOffering.FromId(context, request.WpsId);

                List<int> ids = wps.GetGroupsWithPrivileges();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    result.Add(new WebGroup(grp));
                }

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(WpsServiceAddGroupRequestTep request) {

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                WpsProcessOffering wps = (WpsProcessOffering)WpsProcessOffering.FromId(context, request.WpsId);

                List<int> ids = wps.GetGroupsWithPrivileges();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    if(grp.Id == request.Id) return new WebResponseBool(false);
                }

                wps.StorePrivilegesForGroups(new int[]{request.Id});

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Delete(WpsServiceDeleteGroupRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();

                //TODO: replace once http://project.terradue.com/issues/13954 is resolved
                string sql = String.Format("DELETE FROM service_priv WHERE id_service={0} AND id_grp={1};",request.WpsId, request.Id);
                context.Execute(sql);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(WpsServiceGetRequestTep request) {
            WebServiceTep result;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                WpsProcessOffering wps = (WpsProcessOffering)WpsProcessOffering.FromId(context, request.Id);
                result = new WebServiceTep(context, wps);
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }
    }

}

