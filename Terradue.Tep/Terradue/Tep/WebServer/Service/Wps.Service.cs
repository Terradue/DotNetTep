using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Web;
using System.Xml;
using OpenGis.Wps;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
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
            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Open();
            context.LogInfo(this,string.Format("/cr/wps/search GET"));

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
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            object result;
            context.Open();
            context.LogInfo(this,string.Format("/service/wps/search GET"));

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;
            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);

            //Create EntityList from DB
            EntityList<WpsProcessOffering> wpsProcesses = new EntityList<WpsProcessOffering>(context);
            wpsProcesses.SetFilter("Available", "true");
            wpsProcesses.OpenSearchEngine = ose;

            CloudWpsFactory wpsOneProcesses = new CloudWpsFactory(context);
            wpsOneProcesses.OpenSearchEngine = ose;
            wpsProcesses.Identifier = wpsOneProcesses.Identifier;

            wpsProcesses.Identifier = "service/wps";
            var entities = new List<IOpenSearchable> { wpsProcesses, wpsOneProcesses };

            MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(entities, ose);
            IOpenSearchResultCollection osr = ose.Query(multiOSE, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(wpsProcesses, osr);

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }

        public object Get (GetWPSProcessDescription request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/job/wps/description GET"));

                EntityList<WpsProcessOffering> wpsservices = new EntityList<WpsProcessOffering> (context);
                wpsservices.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = wpsservices.GetOpenSearchDescription ();

                context.Close ();

                return new HttpResult (osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }
        }


        public object Get(GetWPSServices request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            List<WebServiceTep> result = new List<WebServiceTep>();
            try {
                context.Open();
                context.LogInfo(this,string.Format("/service/wps GET"));

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
                        foreach (WpsProcessOffering process in wps.GetWpsProcessOfferingsFromRemote()) {
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
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }

            return result;
        }

        public object Get(GetWebProcessingServices request) {
            IfyWebContext context;
            System.IO.Stream stream = new System.IO.MemoryStream();
            context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            context.AccessLevel = EntityAccessLevel.Administrator;

            context.Open();
            context.LogInfo(this,string.Format("/wps/WebProcessingService GET service='{4}',request='{2}',version='{5}',identifier='{1}',dataInputs='{0}',responseDocument='{3}'",
                                              request.DataInputs, request.Identifier, request.Request, request.ResponseDocument, request.Service, request.Version));

            if (string.IsNullOrEmpty(request.Service) || string.IsNullOrEmpty(request.Request) || request.Service.ToLower() != "wps")
                throw new Exception("Web Processing Service Request is not valid");
            WpsProcessOffering wps = null;
            switch (request.Request.ToLower()) {
                case "getcapabilities":
                    context.LogDebug(this,string.Format("WPS GetCapabilities requested"));
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
                    context.LogDebug(this,string.Format("WPS DescribeProcess requested"));
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
                    context.LogDebug(this,string.Format("WPS Execute requested"));
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
                    context.LogError(this, "Web Processing Service Request is not valid");
                    context.Close();
                    throw new Exception("Web Processing Service Request is not valid");
            }

        }

        public object Post(WpsExecutePostRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            object response = null;
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/wps/WebProcessingService POST"));

                var executeInput = (Execute)new System.Xml.Serialization.XmlSerializer (typeof (Execute)).Deserialize (request.RequestStream);
                context.LogDebug (this, string.Format ("Deserialization done"));
                response = Execute (context, executeInput);
                if (response is double) response = new WebResponseString(response.ToString());
            } catch (Exception e){
                context.Close ();
                return new HttpError (e.Message);
            }
            context.Close();
            return response;
        }

        private object Execute(IfyContext context, Execute executeInput){
            WpsProcessOffering wps = CloudWpsFactory.GetWpsProcessOffering(context, executeInput.Identifier.Value);
            object executeResponse = null;
            var stream = new System.IO.MemoryStream();
            ObjectCache cache = MemoryCache.Default;
            WpsJob wpsjob = null;

            try{
                var parameters = BuildWpsJobParameters(context, executeInput);
                bool quotationMode = false;
                bool isQuotable = false;
                string cachekey = wps.Identifier;

                //check if the service is quotable (=has quotation parameter)
                foreach (var p in parameters) {
                    if (p.Key == "quotation") {
                        isQuotable = true;
                        if (p.Value == "true") quotationMode = true;
                    } else cachekey += p.Key + p.Value;
                }

                //part is quotable
                if (isQuotable) {
                    cachekey = "quotation-" + CalculateMD5Hash(cachekey);

                    if (quotationMode) {
                        //we do a quotation request to the WPS service and then put the calculated estimation in the response
                        //calculation is done from the rates associated to the wps service

                        var policy = new CacheItemPolicy();
                        policy.SlidingExpiration = new TimeSpan(0, 0, 10, 0);//we keep it 10min in memory, then user must do a new one

                        executeResponse = wps.Execute(executeInput);

                        if (!(executeResponse is ExecuteResponse)) return HandleWrongExecuteResponse(context, executeResponse);

                        var executeresponse = executeResponse as ExecuteResponse;
                        foreach (var processOutput in executeresponse.ProcessOutputs) {
                            if (processOutput.Identifier != null && processOutput.Identifier.Value == "QUOTATION") {
                                var data = processOutput.Item as DataType;
                                var cdata = data != null ? data.Item as ComplexDataType : null;
                                var json = cdata.Text;
                                var accountings = JsonSerializer.DeserializeFromString<List<T2Accounting>>(json);
                                if (accountings.Count == 0) throw new Exception("Wrong Execute response to quotation");
                                //calculate estimation with rates
                                double estimation = Rates.GetBalanceFromRates(context, wps, accountings[0].quantity);
								cache.Set(cachekey, estimation.ToString(), policy);
                                var ldata = new LiteralDataType();
                                ldata.Value = estimation.ToString();
                                data.Item = ldata;

                                new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExecuteResponse)).Serialize(stream, executeresponse);
                                return new HttpResult(stream, "application/xml");
                            }
                        }
                        throw new Exception("Wrong Execute response to quotation");
                    } else {
                        //we do a normal Execute request to a wps service which is quotable
                        //it means that the quotation must have been done (found in cache) and that user has enough credit
						var quotation = cache[cachekey] as string;
                        if (string.IsNullOrEmpty(quotation)) throw new Exception("Unable to read the quotation, please do a new one.");

                        var user = UserTep.FromId(context, context.UserId);
                        var balance = user.GetAccountingBalance();
                        if (double.Parse(quotation) > balance) throw new Exception("User credit insufficiant for this request.");
                        wpsjob = CreateJobFromExecuteInput(context, wps, executeInput);
                        executeResponse = wps.Execute(executeInput, wpsjob.Identifier);

                        if (!(executeResponse is ExecuteResponse)) return HandleWrongExecuteResponse(context, executeResponse);

                        //We store the accounting deposit
                        if (string.IsNullOrEmpty(quotation)) throw new Exception("Unable to read the quotation, please do a new one.");
                        var transaction = new Transaction(context);
                        transaction.Entity = wpsjob;
                        transaction.OwnerId = context.UserId;
                        transaction.Identifier = wpsjob.Identifier;
                        transaction.LogTime = DateTime.UtcNow;
                        transaction.ProviderId = wps.OwnerId;
                        transaction.Balance = double.Parse(quotation);
                        transaction.Deposit = true;
                        transaction.Store();
                    }
                } else { 
                    //case is not quotable
                    wpsjob = CreateJobFromExecuteInput(context, wps, executeInput);
                    executeResponse = wps.Execute(executeInput);
                }

                if (!(executeResponse is ExecuteResponse)) return HandleWrongExecuteResponse(context, executeResponse);
                 
                context.LogDebug(this,string.Format("Execute response ok"));
                var execResponse = executeResponse as ExecuteResponse;

                UpdateJobFromExecuteResponse(context, ref wpsjob, execResponse);

                Uri uri = new Uri(execResponse.serviceInstance);
                execResponse.serviceInstance = context.BaseUrl + uri.PathAndQuery;
                execResponse.statusLocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + wpsjob.Identifier;
                new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExecuteResponse)).Serialize(stream, execResponse);

                return new HttpResult(stream, "application/xml");

            }catch(Exception e){
                context.LogError(this, e.Message);
                return new HttpError(e.Message);
            }
        }

        private object HandleWrongExecuteResponse(IfyContext context, object executeResponse) { 
            if (executeResponse is ExceptionReport) {
                var stream = new System.IO.MemoryStream();
                context.LogDebug(this, string.Format("Exception report ok"));
                var exceptionReport = executeResponse as ExceptionReport;
                new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExceptionReport)).Serialize(stream, exceptionReport);
                stream.Seek(0, SeekOrigin.Begin);
                using (StreamReader reader = new StreamReader(stream)) {
                    string errormsg = reader.ReadToEnd();
                    context.LogError(this, string.Format(errormsg));
                    return new HttpError(errormsg, HttpStatusCode.BadRequest, exceptionReport.Exception[0].exceptionCode, exceptionReport.Exception[0].ExceptionText[0]);
                }
            } else {
                return new HttpError("Unknown error during the Execute", HttpStatusCode.BadRequest, "NoApplicableCode", "");
            }
        }

        private string CalculateMD5Hash(string input){
            var md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("X2"));
            return sb.ToString();
        }


        private List<T2Accounting> GetAccountingsFromExecute(ExecuteResponse executeresponse) {
            foreach (var processOutput in executeresponse.ProcessOutputs) {
                if (processOutput.Identifier != null && processOutput.Identifier.Value == "QUOTATION") {
                    var data = processOutput.Item as DataType;
                    var cdata = data != null ? data.Item as ComplexDataType : null;
                    var json = cdata.Text;
                    var accountings = JsonSerializer.DeserializeFromString<List<T2Accounting>>(json);
                    return accountings;
                }
            }
            return new List<T2Accounting>();
        }

        private WpsJob CreateJobFromExecuteInput(IfyContext context, WpsProcessOffering wps, Execute executeInput){
            context.LogDebug(this, string.Format("Creating job from execute request"));
            string newId = Guid.NewGuid().ToString();

            //create WpsJob
            context.LogDebug(this,string.Format("Provider is null ? -> " + (wps.Provider == null ? "true" : "false")));
            WpsJob wpsjob = new WpsJob(context);
            wpsjob.Name = wps.Name;
            wpsjob.Identifier = newId;
            wpsjob.OwnerId = context.UserId;
            wpsjob.UserId = context.UserId;
            wpsjob.WpsId = wps.Provider.Identifier;
            wpsjob.ProcessId = wps.Identifier;
            wpsjob.CreatedTime = DateTime.UtcNow;

            wpsjob.Parameters = new List<KeyValuePair<string, string>>();
            wpsjob.Parameters = BuildWpsJobParameters(context, executeInput);

            return wpsjob;
        }

        private void UpdateJobFromExecuteResponse(IfyContext context, ref WpsJob wpsjob, ExecuteResponse execResponse) {
            context.LogDebug(this, string.Format("Creating job from execute response"));
            Uri uri = new Uri(execResponse.statusLocation);

            //create WpsJob
            context.LogDebug(this, string.Format("Get identifier from status location"));
            string identifier = null;
            var pos = uri.Query != null ? uri.Query.ToLower().IndexOf("id=") : 0;
            if (pos > 0) identifier = uri.Query.Substring(pos + 3);
            else {
                context.LogDebug(this, string.Format("identifier does not contain id="));
                //statusLocation url is different for gpod
                if (uri.AbsoluteUri.Contains("gpod.eo.esa.int")) {
                    context.LogDebug(this, string.Format("identifier taken from gpod url : " + uri.AbsoluteUri));
                    identifier = uri.AbsoluteUri.Substring(uri.AbsoluteUri.LastIndexOf("status") + 7);
                } else if (uri.AbsoluteUri.Contains("pywps")) {
                    identifier = uri.AbsoluteUri;
                    identifier = identifier.Substring(identifier.LastIndexOf("pywps-") + 6);
                    identifier = identifier.Substring(0, identifier.LastIndexOf(".xml"));
                }
            }
            context.LogDebug(this, string.Format("identifier = " + identifier));
            wpsjob.RemoteIdentifier = identifier;

            //in case of username:password in the provider url, we take them from provider
            var statusuri = new UriBuilder(wpsjob.Provider.BaseUrl);
            var statusuri2 = new UriBuilder(execResponse.statusLocation);
            statusuri2.UserName = statusuri.UserName;
            statusuri2.Password = statusuri.Password;
            wpsjob.StatusLocation = statusuri2.Uri.AbsoluteUri;

            wpsjob.Store();
        }

        private List<KeyValuePair<string, string>> BuildWpsJobParameters(IfyContext context, Execute executeInput){
            context.LogDebug(this, string.Format("Building job parameters from execute request"));
            List<KeyValuePair<string, string>> output = new List<KeyValuePair<string, string>>();
            foreach (var d in executeInput.DataInputs) {
                context.LogDebug(this,string.Format("Input: " + d.Identifier.Value));
                if (d.Data != null && d.Data.Item != null) {
                    if (d.Data.Item is LiteralDataType) {
                        context.LogDebug(this,string.Format("Value is LiteralDataType"));
                        output.Add(new KeyValuePair<string, string>(d.Identifier.Value, ((LiteralDataType)(d.Data.Item)).Value));  
                    } else if (d.Data.Item is ComplexDataType) {
                        context.LogDebug(this, string.Format("Value is ComplexDataType"));
                        throw new Exception("Data Input ComplexDataType not yet implemented");
                    } else if (d.Data.Item is BoundingBoxType) {
                        //for BoundingBoxType, webportal creates LowerCorner and UpperCorner
                        //we just need to save both values as a concatained string
                        context.LogDebug(this, string.Format("Value is BoundingBoxType"));
                        var bbox = d.Data.Item as BoundingBoxType;
                        var bboxVal = (bbox != null && bbox.UpperCorner != null && bbox.LowerCorner != null) ? bbox.LowerCorner.Replace(" ", ",") + "," + bbox.UpperCorner.Replace(" ", ",") : "";
                        output.Add(new KeyValuePair<string, string>(d.Identifier.Value, bboxVal));  
                    } else {
                        throw new Exception("unhandled type of Data");
                    } 
                } else if (d.Reference != null){
                    context.LogDebug(this, string.Format("Value is InputReferenceType"));
                    if (!string.IsNullOrEmpty(d.Reference.href)) {
                        output.Add(new KeyValuePair<string, string>(d.Identifier.Value, d.Reference.href));
                    } else if (d.Reference.Item != null){
                        throw new Exception("Data Input InputReferenceType item not yet implemented");
                    }
                }
            }
            return output;
        }

        public object Get(GetResultsServlets request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.AccessLevel = EntityAccessLevel.Administrator;
            System.IO.Stream stream = new System.IO.MemoryStream();
            try{
                context.Open();
                context.LogInfo(this,string.Format("/wps/RetrieveResultServlet GET Id='{0}'", request.Id));

                //load job from request identifier
                WpsJob wpsjob = WpsJob.FromIdentifier(context, request.Id);
                context.LogDebug(this,string.Format("Get Job {0} status info",wpsjob.Identifier));
                ExecuteResponse execResponse = null;

                var jobresponse = wpsjob.GetStatusLocationContent ();
                if (jobresponse is HttpResult) return jobresponse;
                else if (jobresponse is ExecuteResponse) execResponse = jobresponse as ExecuteResponse;
                else throw new Exception ("Error while creating Execute Response of job " + wpsjob.Identifier);

                //save job status in activity
                try {
                    if (execResponse.Status != null && execResponse.Status.Item != null) {
                        ActivityTep activity = ActivityTep.FromEntityAndPrivilege(context, wpsjob, EntityOperationType.Create);
                        var activityParams = activity.GetParams();
                        if (activityParams == null || activityParams["status"] == null) {
                            if (execResponse.Status.Item is ProcessSucceededType) {
                                activity.AddParam("status", "succeeded");
                                activity.Store();
                            } else if (execResponse.Status.Item is ProcessFailedType) {
                                activity.AddParam("status", "failed");
                                activity.Store();
                            }
                        }
                    }
                } catch (Exception) { }

                if(string.IsNullOrEmpty(execResponse.statusLocation)) execResponse.statusLocation = wpsjob.StatusLocation;

                execResponse.statusLocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + wpsjob.Identifier;

                var jobResultUrl = context.BaseUrl + "/job/wps/" + wpsjob.Identifier + "/products/description";

                if (execResponse.ProcessOutputs != null) {
                    foreach (OutputDataType output in execResponse.ProcessOutputs) {
                        try{
                            if (output.Identifier != null && output.Identifier.Value != null) { 
                                context.LogDebug (this, string.Format ("Case {0}", output.Identifier.Value));
                                if (output.Identifier.Value.Equals ("result_metadata") || output.Identifier.Value.Equals ("result_osd")) {

                                    if (output.Item is DataType && ((DataType)(output.Item)).Item != null) {
                                        var item = ((DataType)(output.Item)).Item as ComplexDataType;
                                        var reference = item.Reference as OutputReferenceType;
                                        reference.href = jobResultUrl;
                                        reference.mimeType = "application/opensearchdescription+xml";
                                        item.Reference = reference;
                                        ((DataType)(output.Item)).Item = item;
                                    } else if (output.Item is OutputReferenceType) {
                                        context.LogDebug (this, string.Format ("Case result_osd"));
                                        var reference = output.Item as OutputReferenceType;
                                        reference.href = jobResultUrl;
                                        reference.mimeType = "application/opensearchdescription+xml";
                                        output.Item = reference;
                                    }

                                    output.Identifier = new CodeType { Value = "result_osd" };
                                } else {
                                    if (output.Item is DataType && ((DataType)(output.Item)).Item != null) {
                                        var item = ((DataType)(output.Item)).Item as ComplexDataType;
                                        if (item.Any != null) {
                                            var reference = new OutputReferenceType ();
                                            reference.mimeType = "application/opensearchdescription+xml";
                                            reference.href = jobResultUrl;
                                            item.Reference = reference;
                                            item.Any = null;
                                            item.mimeType = "application/xml";
                                            output.Identifier = new CodeType { Value = "result_osd" };
                                        }
                                    }
                                }
                            }
                        }catch(Exception e){
                            context.LogError (this, e.Message);
                        }
                    }
                }
                Uri uri = new Uri(execResponse.serviceInstance);
                execResponse.serviceInstance = context.BaseUrl + uri.PathAndQuery;
                new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExecuteResponse)).Serialize(stream, execResponse);
                context.Close();

                var result = new HttpResult(stream, "application/xml");

                result.Headers.Remove("Cache-Control");
                result.Headers.Add("Cache-Control", "max-age=60");

                return result;

            }catch(Exception e){
                context.LogError(this, e.Message);
                return new HttpResult(e.Message, HttpStatusCode.BadRequest);
            }
        }

        public object Get(GetWPSProviders request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            context.AccessLevel = EntityAccessLevel.Administrator;
            List<WebWpsProvider> result = new List<WebWpsProvider>();
            try {
                context.Open();
                context.LogInfo(this,string.Format("/cr/wps GET"));

                EntityList<WpsProvider> wpsProviders = new EntityList<WpsProvider>(context);
                wpsProviders.Load();

                foreach (WpsProvider wps in wpsProviders) {
                    result.Add(new WebWpsProvider(wps));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }

            return result;
        }

        public object Post(CreateWPSProvider request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
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

                context.LogInfo(this,string.Format("/cr/wps POST Id='{0}'", wpsProvider.Id));

                //Make it public, the authorizations will then be done on the services
                wpsProvider.GrantPermissionsToAll();

                wpsProvider.StoreProcessOfferings();

                result = new WebWpsProvider(wpsProvider);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Post(CreateWPSService request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
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

                context.LogInfo(this,string.Format("/service/wps POST Id='{0}'", wps.Id));

                result = new WebWpsService(wps);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Delete(DeleteWPSService request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            bool result = false;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/service/wps DELETE Id='{0}'", request.Id));

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
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(result);
        }

        public object Delete(DeleteWPSProvider request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            bool result = false;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/cr/wps DELETE Id='{0}'", request.Id));

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
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(result);
        }

        public object Put(UpdateWPSProvider request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebWpsProvider result = null;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/cr/wps PUT Id='{0}'", request.Id));

                WpsProvider wps = (request.Id == 0 ? null : (WpsProvider)WpsProvider.FromId(context, request.Id));

                if (!string.IsNullOrEmpty(request.Mode) && request.Mode.Equals("synchro"))
                    wps.UpdateProcessOfferings();
                else {
                    var namebefore = wps.Identifier;
                    wps = request.ToEntity(context, wps);
                    wps.Store();

                    //update wpsjob if name has changed
                    if(!namebefore.Equals(wps.Identifier)){
                        EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
                        wpsjobs.Load();
                        foreach(var job in wpsjobs){
                            job.WpsId = wps.Identifier;
                            job.Store();
                        }
                    }
                }

                result = new WebWpsProvider(wps);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(WpsServiceUpdateRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebServiceTep result = null;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/service/wps PUT Id='{0}'", request.Id));

                WpsProcessOffering wps = (request.Id == 0 ? null : (WpsProcessOffering)WpsProcessOffering.FromId(context, request.Id));

                if(request.Access != null){
                    switch(request.Access){
                        case "public":
                        wps.GrantPermissionsToAll();
                            break;
                        case "private":
                            wps.RevokePermissionsFromAll(true, false);
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
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        private static OpenGis.Wps.WPSCapabilitiesType CreateGetCapabilititesTemplate(string baseUrl) {
            OpenGis.Wps.WPSCapabilitiesType capabilitites = new OpenGis.Wps.WPSCapabilitiesType();

            capabilitites.ServiceIdentification = new OpenGis.Wps.ServiceIdentification();
            capabilitites.ServiceIdentification.Title = new List<OpenGis.Wps.LanguageStringType>{ new OpenGis.Wps.LanguageStringType{ Value = "Tep WPS" } };
            capabilitites.ServiceIdentification.Abstract = new List<OpenGis.Wps.LanguageStringType>{ new OpenGis.Wps.LanguageStringType{ Value = "Proxy WPS of the TEP platform" } };
            capabilitites.ServiceIdentification.Keywords = new List<OpenGis.Wps.KeywordsType>();
            OpenGis.Wps.KeywordsType kw1 = new OpenGis.Wps.KeywordsType{ Keyword = new List<OpenGis.Wps.LanguageStringType>{ new OpenGis.Wps.LanguageStringType{ Value = "WPS" } } };
            List<OpenGis.Wps.LanguageStringType> listKeywords = new List<OpenGis.Wps.LanguageStringType>();
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "WPS" });
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "TEP" });
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "geospatial" });
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "geoprocessing" });
            capabilitites.ServiceIdentification.Keywords.Add(new OpenGis.Wps.KeywordsType{ Keyword = listKeywords });
            capabilitites.ServiceIdentification.ServiceType = new OpenGis.Wps.CodeType{ Value = "WPS" };
            capabilitites.ServiceIdentification.ServiceTypeVersion = new List<string>{ "1.0.0" };
            capabilitites.ServiceIdentification.Fees = "None";
            capabilitites.ServiceIdentification.AccessConstraints = new List<string>{ "NONE" };

            capabilitites.ServiceProvider = new OpenGis.Wps.ServiceProvider();
            capabilitites.ServiceProvider.ProviderName = "Tep";
            capabilitites.ServiceProvider.ProviderSite = new OpenGis.Wps.OnlineResourceType{ href = "https://tep.eo.esa.int/" };

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

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/service/wps/{{wpsId}}/group GET WpsId='{0}'", request.WpsId));

                WpsProcessOffering wps = (WpsProcessOffering)WpsProcessOffering.FromId(context, request.WpsId);

                List<int> ids = wps.GetAuthorizedGroupIds().ToList();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    result.Add(new WebGroup(grp));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/service/wps/{{wpsId}}/group POST WpsId='{0}'", request.WpsId));
                WpsProcessOffering wps = (WpsProcessOffering)WpsProcessOffering.FromId(context, request.WpsId);

                List<int> ids = wps.GetAuthorizedGroupIds().ToList();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    if(grp.Id == request.Id) return new WebResponseBool(false);
                }

                wps.GrantPermissionsToGroups(new int[]{request.Id});

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/service/wps/{{wpsId}}/group/{{Id}} DELETE WpsId='{0}',Id='{1}'", request.WpsId, request.Id));

                //TODO: replace once http://project.terradue.com/issues/13954 is resolved
                string sql = String.Format("DELETE FROM service_perm WHERE id_service={0} AND id_grp={1};",request.WpsId, request.Id);
                context.Execute(sql);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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

            var context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/service/wps/{{Id}} GET Id='{0}'", request.Id));
                WpsProcessOffering wps = (WpsProcessOffering)WpsProcessOffering.FromId(context, request.Id);
                result = new WebServiceTep(context, wps);
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

