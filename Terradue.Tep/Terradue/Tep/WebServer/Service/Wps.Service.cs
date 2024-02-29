using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Threading;
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

        static readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

        public object Post(ValidateWPSService request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);

            if (string.IsNullOrEmpty(request.Identifier)) {
                var segments = base.Request.PathInfo.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                request.Identifier = segments[segments.Count() - 2];
            }
            if (string.IsNullOrEmpty(request.Identifier)) return null;            
            
            string response = null;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{0}/validate POST", request.Identifier));
                WpsProcessOfferingTep service = WpsProcessOfferingTep.FromIdentifier(context, request.Identifier);                
                
                string json = "";
                using(StreamReader reader = new StreamReader(request.RequestStream)){
                    json = reader.ReadToEnd();
                }
                if(string.IsNullOrEmpty(json)) return null;

                if(!json.Contains("t2_authorization_bearer")){
                    var user = UserTep.FromId(context, context.UserId);                    
                    json = json.Remove(json.Length - 1);
                    json += "," + "\"t2_authorization_bearer\":\"" + user.GetSessionApiKey() + "\"}";
                }
                if(!json.Contains("t2_username")){
                    var user = UserTep.FromId(context, context.UserId);                                        
                    if (string.IsNullOrEmpty(user.TerradueCloudUsername)) user.LoadCloudUsername();
                    if (!string.IsNullOrEmpty(user.TerradueCloudUsername)){
                        json = json.Remove(json.Length - 1);
                        json += "," + "\"t2_username\":\"" + user.TerradueCloudUsername + "\"}";
                    }
                }
                
                response = service.ValidateResult(json);
            } catch (Exception e) {
                context.Close();
                return new HttpError(e.Message);
            }
            context.Close();
            return response;
        }

        public object Get(SearchWPSProviders request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
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

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest.QueryString, httpRequest.Headers, ose);
            IOpenSearchResultCollection osr = ose.Query(wpsProviders, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(wpsProviders, osr);
            //            OpenSearchFactory.ReplaceSelfLinks(wpsProviders, httpRequest.QueryString, osr.Result, EntrySelfLinkTemplate );

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }

        public object Get(SearchWPSServices request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();
            context.LogInfo(this,string.Format("/service/wps/search GET"));

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;
            var qs = new NameValueCollection(httpRequest.QueryString);
            if(string.IsNullOrEmpty(qs["available"])) qs.Set("available","true");
            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest.QueryString, httpRequest.Headers, ose);

            //Create EntityList from DB
            EntityList<WpsProcessOffering> wpsProcesses = new EntityList<WpsProcessOffering>(context);
            wpsProcesses.OpenSearchEngine = ose;
            wpsProcesses.Identifier = string.Format("servicewps-{0}", context.Username);                        
            wpsProcesses.AddSort("Name",SortDirection.Descending);
            wpsProcesses.AddSort("Version");

            // CloudWpsFactory wpsOneProcesses = new CloudWpsFactory(context);
            // wpsOneProcesses.OpenSearchEngine = ose;
            //wpsProcesses.Identifier = wpsOneProcesses.Identifier;

            wpsProcesses.Identifier = "service/wps";
            // var entities = new List<IOpenSearchable> { wpsProcesses, wpsOneProcesses };

            if (!string.IsNullOrEmpty(qs["cache"]) && qs["cache"] == "false" && MasterCatalogue.SearchCache != null){                
                MasterCatalogue.SearchCache.ClearCache(".*", DateTime.Now);
            }

            var settings = MasterCatalogue.OpenSearchFactorySettings;
            // MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(entities, settings);
            // IOpenSearchResultCollection osr = ose.Query(multiOSE, qs, responseType);
            IOpenSearchResultCollection osr = ose.Query(wpsProcesses, qs, responseType);

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

                EntityList<WpsProcessOfferingTep> wpsservices = new EntityList<WpsProcessOfferingTep> (context);
                wpsservices.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = wpsservices.GetOpenSearchDescription ();

                context.Close ();

                return new HttpResult (osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }
        }


        public object Get(GetWPSServices request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            List<WebServiceTep> result = new List<WebServiceTep>();
            try {
                context.Open();
                context.LogInfo(this,string.Format("/service/wps GET"));

                EntityList<WpsProcessOffering> services = new EntityList<WpsProcessOffering>(context);
                services.Load();

                if (request.Cloud) {

                    //+ WPS from cloud
                    List<WpsProvider> wpss = new List<WpsProvider>();
                    try {
                        CloudWpsFactory wpsFinder = new CloudWpsFactory(context);
                        wpss = wpsFinder.GetWPSFromVMs();
                    } catch (Exception) {
                        //we do nothing, we will return the list without any WPS from the cloud
                    }
                    foreach (WpsProvider wps in wpss) {
                        try {
                            foreach (WpsProcessOffering process in wps.GetWpsProcessOfferingsFromRemote()) {
                                process.UserId = 0;
                                services.Include(process);
                            }
                        } catch (Exception) {
                            //we do nothing, we just dont add the process
                        }
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
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }

            return result;
        }

        public object Get(GetWebProcessingServices request) {
            IfyWebContext context;
            System.IO.Stream stream = new System.IO.MemoryStream();
            context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            context.AccessLevel = EntityAccessLevel.Administrator;

            context.Open();
            context.LogInfo(this,string.Format("/wps/WebProcessingService GET service='{4}',request='{2}',version='{5}',identifier='{1}',dataInputs='{0}',responseDocument='{3}'",
                                              request.DataInputs, request.Identifier, request.Request, request.ResponseDocument, request.Service, request.Version));

            if (string.IsNullOrEmpty(request.Service) || string.IsNullOrEmpty(request.Request) || request.Service.ToLower() != "wps")
                throw new Exception("Web Processing Service Request is not valid");
            WpsProcessOfferingTep wps = null;
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
                            descResponse = WpsFactory.DescribeProcessCleanup(context, descResponse);
                            System.Xml.Serialization.XmlSerializerNamespaces ns = new System.Xml.Serialization.XmlSerializerNamespaces();
                            ns.Add("wps", "http://www.opengis.net/wps/1.0.0");
                            ns.Add("ows","http://www.opengis.net/ows/1.1");
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
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
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

        /// <summary>
        /// Reads the cache.
        /// </summary>
        /// <returns>The cache.</returns>
        /// <param name="key">Key.</param>
        private string ReadCache(string key, IfyContext context = null) {
            if (context != null) context.LogDebug(this, string.Format("Read Cache - {0}", key));
            //First we do a read lock to see if it already exists, this allows multiple readers at the same time.
            cacheLock.EnterReadLock();
            try {
                //Returns null if the string does not exist, prevents a race condition where the cache invalidates between the contains check and the retreival.
                var cachedString = MemoryCache.Default.Get(key, null) as string;

                if (cachedString != null) {
                    if (context != null) context.LogDebug(this, string.Format("Cache found : {0}", cachedString));
                    return cachedString;
                }
                if (context != null) context.LogDebug(this, string.Format("Cache not found"));
            } finally {
                cacheLock.ExitReadLock();
            }
            return null;
        }

        /// <summary>
        /// Writes the cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        private void WriteCache(string key, string value, IfyContext context = null){
            if (context != null) context.LogDebug(this, string.Format("Write Cache - {0} - {1}",key,value));

            //First we do a read lock to see if it already exists, this allows multiple readers at the same time.
            cacheLock.EnterReadLock();
            try {
                //Returns null if the string does not exist, prevents a race condition where the cache invalidates between the contains check and the retreival.
                var cachedString = MemoryCache.Default.Get(key, null) as string;
                if (cachedString != null) {
                    if (context != null) context.LogDebug(this, string.Format("Cache already exists, we do not create it"));
                    return;
                }
            } finally {
                cacheLock.ExitReadLock();
            }

            //Only one UpgradeableReadLock can exist at one time, but it can co-exist with many ReadLocks
            cacheLock.EnterUpgradeableReadLock();
            try {
                //We need to check again to see if the string was created while we where waiting to enter the EnterUpgradeableReadLock
                var cachedString = MemoryCache.Default.Get(key, null) as string;
                if (cachedString != null) {
                    if (context != null) context.LogDebug(this, string.Format("Cache already exists (2), we do not create it"));
                    return;
                }

                //The entry still does not exist so we need to create it and enter the write lock
                cacheLock.EnterWriteLock(); //This will block till all the Readers flush.
                try {
                    CacheItemPolicy cip = new CacheItemPolicy() {
                        AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(1))
                    };
                    MemoryCache.Default.Set(key, value, cip);
                    if (context != null) context.LogDebug(this, string.Format("Cache added"));
                    return;
                } finally {
                    cacheLock.ExitWriteLock();
                }
            } finally {
                cacheLock.ExitUpgradeableReadLock();
            }
        }

        private object Execute(IfyContext context, Execute executeInput){
            WpsProcessOfferingTep wps = CloudWpsFactory.GetWpsProcessOffering(context, executeInput.Identifier.Value);
            object executeResponse = null;
            var stream = new System.IO.MemoryStream();
            WpsJob wpsjob = null;

            double cost = 0;

            try{
                var user = UserTep.FromId(context, context.UserId);
                var parameters = WpsJob.BuildWpsJobParameters(context, executeInput);
                bool accountingEnabled = context.GetConfigBooleanValue("accounting-enabled");
                bool quotationMode = false;
                bool isQuotable = false;
                string cachekey = wps.Identifier;

                wpsjob = WpsJob.CreateJobFromExecuteInput(context, wps, executeInput, parameters);

                wpsjob.CheckCanProcess();
                
                //Check if we need to remove special fields
                if (executeInput != null && executeInput.DataInputs != null) {
                    var tmpInputs = new List<InputType>();
                    foreach (var input in executeInput.DataInputs) {
                        switch(input.Identifier.Value){
                            case "_T2InternalJobTitle":
                                break;
                            default:
                                tmpInputs.Add(input);
                                break;
                        }
                    }
                    executeInput.DataInputs = tmpInputs;
                }

                //Check if it need to add back hidden field
                var describeResponse = wps.DescribeProcess();
				if (describeResponse is ProcessDescriptions) {
                    var descResponse = describeResponse as ProcessDescriptions;
					if (WpsFactory.DescribeProcessHasField(descResponse, "_T2InternalJobTitle")) {
						var input = new InputType();
						input.Identifier = new CodeType { Value = "_T2InternalJobTitle" };
						input.Data = new DataType {
							Item = new LiteralDataType {
								Value = wpsjob.Name
							}
						};
						executeInput.DataInputs.Add(input);
					}
                    if (WpsFactory.DescribeProcessHasField(descResponse, "_T2Username")) {
                        var input = new InputType();
                        input.Identifier = new CodeType { Value = "_T2Username" };
                        input.Data = new DataType {
                            Item = new LiteralDataType {
                                Value = user.TerradueCloudUsername
                            }
                        };
                        executeInput.DataInputs.Add(input);
                    }
                    if (WpsFactory.DescribeProcessHasField(descResponse, "_T2UserEmail")) {
                        var input = new InputType();
                        input.Identifier = new CodeType { Value = "_T2UserEmail" };
                        input.Data = new DataType {
                            Item = new LiteralDataType {
                                Value = user.Email
                            }
                        };
                        executeInput.DataInputs.Add(input);
                    }
                    if (WpsFactory.DescribeProcessHasField(descResponse, "_T2ApiKey")) {
						var input = new InputType();
						input.Identifier = new CodeType { Value = "_T2ApiKey" };
						input.Data = new DataType {
							Item = new LiteralDataType {
                                Value = user.GetSessionApiKey()
							}
						};
						executeInput.DataInputs.Add(input);
					}
                    // if (WpsFactory.DescribeProcessHasField(descResponse, "_T2JobInfoFeed")) {
                    //     var jobinfofeed = wpsjob.GetJobAtomFeed();
                    //     var jobinfo = ThematicAppCachedFactory.GetOwsContextAtomFeedAsString(jobinfofeed);
                    //     var cdata = jobinfo;
                    //     var input = new InputType();
                    //     input.Identifier = new CodeType { Value = "_T2JobInfoFeed" };
                    //     input.Data = new DataType {
                    //         Item = new LiteralDataType {
                    //             Value = cdata
                    //         }
                    //     };
                    //     executeInput.DataInputs.Add(input);
                    // }
                    if (WpsFactory.DescribeProcessHasField(descResponse, "_T2ResultsAnalysis")) {
                        var analysis = context.GetConfigValue("wpsDefaultValue_T2ResultsAnalysis");
                        if (string.IsNullOrEmpty(analysis)) analysis = "none";
                        var input = new InputType();
                        input.Identifier = new CodeType { Value = "_T2ResultsAnalysis" };
                        input.Data = new DataType {
                            Item = new LiteralDataType {
                                Value = analysis
                            }
                        };
                        executeInput.DataInputs.Add(input);
                    }
                } else {
					return new HttpError("Unknown error during the DescribeProcess", HttpStatusCode.BadRequest, "NoApplicableCode", "");
				}

                if (accountingEnabled) {
                    //check if the service is quotable (=has quotation parameter)
                    foreach (var p in parameters) {
                        if (p.Key == "quotation") {
                            isQuotable = true;
                            if (p.Value == "true" || p.Value == "Yes") quotationMode = true;
                        } else {
                            cachekey += p.Key + p.Value;
                        }
                    }
                }

                //part is quotable
                if (isQuotable) {
                    cachekey = "quotation-" + CalculateMD5Hash(cachekey);

                    if (quotationMode) {
                        //we do a quotation request to the WPS service and then put the calculated estimation in the response
                        //calculation is done from the rates associated to the wps service

                        executeResponse = wps.Execute(executeInput);

                        if (!(executeResponse is ExecuteResponse)) return HandleWrongExecuteResponse(context, executeResponse);

                        var executeresponse = executeResponse as ExecuteResponse;
                        foreach (var processOutput in executeresponse.ProcessOutputs) {
                            if (processOutput.Identifier != null && processOutput.Identifier.Value == "QUOTATION") {
                                var data = processOutput.Item as DataType;
                                var cdata = data != null ? data.Item as ComplexDataType : null;
                                var json = cdata.Text;
                                var accountings = JsonSerializer.DeserializeFromString<List<T2Accounting>>(json);
                                if (string.IsNullOrEmpty(json) || accountings.Count == 0) throw new Exception("Wrong Execute response to quotation");
                                //calculate estimation with rates
                                var estimation = Convert.ToInt32(Rates.GetBalanceFromRates(context, wps.Provider, accountings[0].quantity));
                                WriteCache(cachekey,estimation.ToString(), context);

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
                        var quotation = ReadCache(cachekey, context);
                        if (string.IsNullOrEmpty(quotation)) throw new Exception("Unable to read the quotation, please do a new one.");

                        var balance = user.GetAccountingBalance();
                        if (double.Parse(quotation) > balance) throw new Exception("User credit insufficiant for this request.");
                        //wpsjob = WpsJob.CreateJobFromExecuteInput(context, wps, executeInput, parameters);
                        executeResponse = wps.Execute(executeInput, wpsjob.Identifier);

                        if (!(executeResponse is ExecuteResponse) 
                            || ((executeResponse as ExecuteResponse).Status.Item is ProcessFailedType)
                            || string.IsNullOrEmpty((executeResponse as ExecuteResponse).statusLocation)) return HandleWrongExecuteResponse(context, executeResponse);

                        wpsjob.UpdateJobFromExecuteResponse(context, executeResponse as ExecuteResponse);

                        //We store the accounting deposit
                        if (string.IsNullOrEmpty(quotation)) throw new Exception("Unable to read the quotation, please do a new one.");
                        var transaction = new Transaction(context);
                        transaction.Entity = wpsjob;
                        transaction.OwnerId = context.UserId;
                        transaction.Identifier = wpsjob.Identifier;
                        transaction.LogTime = DateTime.UtcNow;
                        transaction.ProviderId = wps.OwnerId;
                        transaction.Balance = double.Parse(quotation);
                        transaction.Kind = TransactionKind.ActiveDeposit;
                        transaction.Store();
                    }
                } else { 
                    //case is not quotable
                    //wpsjob = WpsJob.CreateJobFromExecuteInput(context, wps, executeInput, parameters);
                    executeResponse = wps.Execute(executeInput);

                    if (!(executeResponse is ExecuteResponse)) return HandleWrongExecuteResponse(context, executeResponse);

                    try{
                        using(StringWriter textWriter = new StringWriter())
                        {
                            new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExecuteResponse)).Serialize(textWriter, executeResponse as ExecuteResponse);
                            context.LogInfo(this, "Execute response : " + textWriter.ToString());
                        }
                    }catch(System.Exception) {}

                    wpsjob.UpdateJobFromExecuteResponse(context, executeResponse as ExecuteResponse);
                }

                context.LogDebug(this, string.Format("Execute response ok"));

                var execResponse = executeResponse as ExecuteResponse;
                Uri uri = new Uri(execResponse.serviceInstance ?? execResponse.statusLocation);
                execResponse.serviceInstance = context.BaseUrl + uri.PathAndQuery;
                execResponse.statusLocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + wpsjob.Identifier;
                new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExecuteResponse)).Serialize(stream, execResponse);

                return new HttpResult(stream, "application/xml");

            }catch(Exception e){
                context.LogError(this, e.Message, e);
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
            } else if (executeResponse is ExecuteResponse) {
                if ((executeResponse as ExecuteResponse).Status.Item is ProcessFailedType) {
                    var processFailed = (executeResponse as ExecuteResponse).Status.Item as ProcessFailedType;
                    string error = "Unknown error during the Execute";
                    try {
                        error = processFailed.ExceptionReport.Exception[0].ExceptionText[0];
                    } catch (Exception) { }
                    return new HttpError(error, HttpStatusCode.BadRequest, "NoApplicableCode", "");
                } else if (string.IsNullOrEmpty((executeResponse as ExecuteResponse).statusLocation)) { 
                    return new HttpError("Missing status location in the Execute response", HttpStatusCode.BadRequest, "NoApplicableCode", "");
                }
            }
            return new HttpError("Unknown error during the Execute", HttpStatusCode.BadRequest, "NoApplicableCode", "");
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

        public object Get(GetResultsServlets request) { 
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.AccessLevel = EntityAccessLevel.Administrator;
            System.IO.Stream stream = new System.IO.MemoryStream();
            try{
                context.Open();
                context.LogInfo(this,string.Format("/wps/RetrieveResultServlet GET Id='{0}'", request.Id));

                bool accountingEnabled = context.GetConfigBooleanValue("accounting-enabled");

                //load job from request identifier
                WpsJob wpsjob = WpsJob.FromIdentifier(context, request.Id);
                context.LogDebug(this,string.Format("Get Job {0} status info ({1})",wpsjob.Identifier,wpsjob.StringStatus));
                ExecuteResponse execResponse = null;                

                if (wpsjob.Status == WpsJobStatus.STAGED) {
                    execResponse = ProductionResultHelper.CreateExecuteResponseForStagedWpsjob(context, wpsjob, execResponse);
                } 
                else if (wpsjob.Status == WpsJobStatus.COORDINATOR && ProductionResultHelper.IsUrlRecastUrl(wpsjob.StatusLocation)){
                    execResponse = ProductionResultHelper.CreateExecuteResponseForStagedWpsjob(context, wpsjob, execResponse);
                }
                else if (wpsjob.Status == WpsJobStatus.FAILED){
                    execResponse = ProductionResultHelper.CreateExecuteResponseForFailedWpsjob(wpsjob);
                }
                // else if (wpsjob.Status == WpsJobStatus.PUBLISHING && wpsjob.OwnerId != context.UserId){
                //     execResponse = ProductionResultHelper.CreateExecuteResponseForPublishingWpsjob(wpsjob);
                // }
                else {
                    object jobresponse;
                    try {
                        jobresponse = wpsjob.UpdateStatus();
                    }catch(Exception esl){
                        throw esl;
                    }

                    if (jobresponse is HttpResult) return jobresponse;                    
                    else if (jobresponse is ExceptionReport) {
                        stream = new System.IO.MemoryStream();
                        context.LogDebug(this, string.Format("Exception report ok"));
                        var exceptionReport = jobresponse as ExceptionReport;
                        new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.ExceptionReport)).Serialize(stream, exceptionReport);
                        stream.Seek(0, SeekOrigin.Begin);
                        using (StreamReader reader = new StreamReader(stream)) {
                            string errormsg = reader.ReadToEnd();
                            context.LogError(this, string.Format(errormsg));
                            return new HttpError(errormsg, HttpStatusCode.BadRequest, exceptionReport.Exception[0].exceptionCode, exceptionReport.Exception[0].ExceptionText[0]);
                        }
                    }
                    else if (jobresponse is ExecuteResponse) execResponse = jobresponse as ExecuteResponse;
                    else throw new Exception("Error while creating Execute Response of job " + wpsjob.Identifier);

                    execResponse.statusLocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + wpsjob.Identifier;

                    //get job recast response
                    try {
						var recastResponse = ProductionResultHelper.GetWpsjobRecastResponse(context, wpsjob, execResponse);
						execResponse = recastResponse;
					}catch(Exception e){
						context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
                return new HttpResult(e.Message, HttpStatusCode.BadRequest);
            }
        }

        public object Get(GetWPSProviders request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
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
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }

            return result;
        }

        public object Get(GetWPSProvider request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            context.AccessLevel = EntityAccessLevel.Administrator;
            WebWpsProvider result = null;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/cr/wps/{{Id}} GET, Id='{0}'", request.Id));

                WpsProvider wps = (WpsProvider)ComputingResource.FromId(context, request.Id);
                result = new WebWpsProvider(wps);                

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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

                //wpsProvider.StoreProcessOfferings(wpsProvider.AutoSync);

                result = new WebWpsProvider(wpsProvider);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                wps.IconUrl = request.IconUrl;
                wps.ValidationUrl = request.ValidationUrl;
                wps.SpecUrl = request.SpecUrl;
                wps.MediaUrl = request.MediaUrl;
                wps.TutorialUrl = request.TutorialUrl;
                wps.PublishType = request.PublishType;
                wps.PublishUrl = request.PublishUrl;

                wps.Provider = wpsProvider;
                wps.Store();

                context.LogInfo(this,string.Format("/service/wps POST Id='{0}'", wps.Id));

                result = new WebWpsService(wps);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Post(CreateWPSProvidersService request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebWpsService result = null;
            try {
                context.Open();

                WpsProviderTep wpsProvider = WpsProviderTep.FromIdentifier(context, request.CrIdentifier);
                WpsProcessOffering wps = (WpsProcessOffering)request.ToEntity(context, new WpsProcessOffering(context));
                wps.Provider = wpsProvider;

                var url = wpsProvider.BaseUrl;
                if(wpsProvider.IsWPS3()){
                    url = request.Url;
                    if (!url.Contains(wps.RemoteIdentifier))
                    {
                        var urib = new UriBuilder(url);
                        urib.Path += "/" + wps.RemoteIdentifier;
                        url = urib.Uri.AbsoluteUri;
                    }
                    //for WPS3 services, we use publish
                    if(string.IsNullOrEmpty(wps.PublishType)) wps.PublishType = System.Configuration.ConfigurationManager.AppSettings["DEFAULT_PUBLISH_TYPE"];
                    if(string.IsNullOrEmpty(wps.PublishUrl)) wps.PublishUrl = System.Configuration.ConfigurationManager.AppSettings["DEFAULT_PUBLISH_URL"];
                    
                } else {
                    if (!url.ToLower().Contains("describeprocess"))
                    {
                        var urib = new UriBuilder(url);
                        var query = "Service=WPS&Request=DescribeProcess&Version=" + wpsProvider.WPSVersion ?? "1.0.0";

                        query += "&Identifier=" + wps.RemoteIdentifier;
                        urib.Query = query;
                        url = urib.Uri.AbsoluteUri;
                    }
                }
                wps.Url = url;
                wps.Store();
                
                context.LogInfo(this, string.Format("/cr/wps/{{CrIdentifier}}/services POST CrIdentifier='{0}', Id='{1}'", request.CrIdentifier, wps.Id));

                result = new WebWpsService(wps);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogInfo(this,string.Format("/service/wps DELETE Identifier='{0}'", request.Identifier));

                WpsProcessOffering wps = null;
                try {
                    wps = (WpsProcessOffering)Service.FromIdentifier(context, request.Identifier);
                    wps.Delete();
                } catch (Exception e) {
                    throw e;
                }

                result = true;

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                try {
                    wps = (WpsProvider)WpsProvider.FromId(context, request.Id);
                    wps.Delete();
                } catch (Exception e) {
                    throw e;
                }

                result = true;

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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

                if (!string.IsNullOrEmpty(request.Mode) && request.Mode.Equals("synchro")){
                    User user = null;
                    if (wps.DomainId != 0) {
                        var role = Role.FromIdentifier(context, RoleTep.OWNER);
                        var usrs = role.GetUsers(wps.DomainId);
                        if (usrs != null && usrs.Length > 0) {
                            user = User.FromId(context, usrs[0]);//we take only the first owner
                        }
                    }
                    wps.CanCache = false;
                    wps.UpdateProcessOfferings(false, user);
                } else {
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
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(GetWPSProvidersServices request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            
            List<Terradue.WebService.Model.WebWpsService> result = new List<Terradue.WebService.Model.WebWpsService>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/cr/wps/{{Identifier}}/services GET, Identifier='{0}'", request.Identifier));

                WpsProviderTep provider = WpsProviderTep.FromIdentifier(context, request.Identifier);
                var services = provider.GetWpsProcessOfferingsFromRemote(true, null);

                foreach (WpsProcessOffering wps in services) {
                    result.Add(new Terradue.WebService.Model.WebWpsService(wps));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogInfo(this,string.Format("/service/wps PUT Identifier='{0}'", request.Identifier));

                WpsProcessOffering wps = (WpsProcessOffering)Service.FromIdentifier(context, request.Identifier);

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
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(GetWPSServicesVersions request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            List<WebServiceTep> result = new List<WebServiceTep>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{Identifier}}/versions GET Identifier='{0}'", request.Identifier));

                WpsProcessOffering wps = (WpsProcessOffering)Service.FromIdentifier(context, request.Identifier);
                var version = string.IsNullOrEmpty(wps.Version) ? "" : wps.Version.Replace("-","_").Replace(".","_");
                var identifierNoVersion = wps.Identifier.Replace(version, "");
                var services = wps.Provider.GetWpsProcessOfferingsFromRemote(true, null);

                foreach (WpsProcessOffering service in services) {
                    var identifierNoVersion2 = service.Identifier.Replace(version, "");
                    if(identifierNoVersion == identifierNoVersion2) result.Add(new WebServiceTep(context, service));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(ReplaceWPSService request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);

            WebWpsService result = null;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{Identifier}}/replace PUT Identifier='{0}', ReplaceIdentifier='{1}'", request.OldIdentifier, request.Identifier));

                WpsProcessOffering wpsOld = (WpsProcessOffering)Service.FromIdentifier(context, request.OldIdentifier);
                WpsProcessOffering wpsNew = (WpsProcessOffering)request.ToEntity(context, new WpsProcessOffering(context));
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

                result = new WebWpsService(wpsNew);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(AddWpsProviderDevUsers request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);

            WebUser result = null;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{Identifier}}/devusers PUT Identifier='{0}', Username='{1}'", request.Identifier, request.Username));

                WpsProvider provider = (Terradue.Portal.WpsProvider)ComputingResource.FromIdentifier(context, request.Identifier);
                provider.AddDevUser(request.Username);

                result = new WebUser(UserTep.FromIdentifier(context, request.Username));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Delete(RemoveWpsProviderDevUsers request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);

            WebUser result = null;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{Identifier}}/devusers PUT Identifier='{0}', Username='{1}'", request.Identifier, request.Username));

                WpsProvider provider = (Terradue.Portal.WpsProvider)ComputingResource.FromIdentifier(context, request.Identifier);
                provider.RemoveDevUser(request.Username);

                result = new WebUser(UserTep.FromIdentifier(context, request.Username));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(GetWpsProviderDevUsers request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);

            List<WebUser> result = new List<WebUser>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{Identifier}}/devusers GET Identifier='{0}'", request.Identifier));

                WpsProvider provider = (Terradue.Portal.WpsProvider)ComputingResource.FromIdentifier(context, request.Identifier);
                
                var users = provider.GetDevUsers();
                foreach (var u in users) result.Add(new WebUser(u));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(GetWpsServicesForCurrentUser request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);

            List<WpsServiceOverview> wpssOverviews = new List<WpsServiceOverview>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/user/current/service/wps"));

                //get all domain where user is a member
                CommunityCollection domains = new CommunityCollection(context);                
                domains.SetFilter("Kind", (int)DomainKind.Public + "," + (int)DomainKind.Private + "," + (int)DomainKind.Hidden);
                domains.Load();
                context.LogDebug(this, string.Format("domain loaded"));

                foreach(var domain in domains){
                    context.LogDebug(this, string.Format("domain " + domain.Identifier));

                    var apps = new EntityList<ThematicApplicationCached>(context);
                    apps.SetFilter("DomainId", domain.Id);
                    apps.Load();
                    context.LogDebug(this, string.Format("apps loaded"));
                    var appsItems = apps.GetItemsAsList();
                    context.LogDebug(this, string.Format("found " + appsItems.Count));
                    foreach(var app in appsItems){
                        var feed = app.Feed;
                        if(feed == null) feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(app.TextFeed);
                        if(feed != null){
                            context.LogDebug(this, string.Format("feed not null - " + feed.Items.Count()));
                            foreach (var item in feed.Items) {
                                context.LogDebug(this, string.Format("get wps services"));
                                //get wps services                            
                                try{
                                var wpsOverviews = ThematicAppFactory.GetWpsServiceOverviews(context, item);
                                foreach(var wps in wpsOverviews)
                                    wps.Community = new CommunityOverview{
                                        Identifier = domain.Identifier,
                                        Title = domain.Name,
                                        Icon = domain.IconUrl,
                                        Status = domain.IsUserJoined(context.UserId) ? "joined" : ""
                                    };
                                wpssOverviews.AddRange(wpsOverviews);
                                }catch(Exception){}
                            }
                        } else context.LogDebug(this, string.Format("feed null"));
                    }



                    // foreach (var appslink in domain.AppsLinks) {
                    //     context.LogDebug(this, string.Format(appslink));
                    //     try {
                    //         var settings = MasterCatalogue.OpenSearchFactorySettings;
                    //         var apps = MasterCatalogue.OpenSearchEngine.Query(new GenericOpenSearchable(new OpenSearchUrl(appslink), settings), new NameValueCollection(), typeof(AtomFeed));
                    //         foreach (IOpenSearchResultItem item in apps.Items) {
                    //             context.LogDebug(this, string.Format("get wps services"));
                    //             //get wps services                            
                    //             var wpsOverviews = ThematicAppFactory.GetWpsServiceOverviews(context, item);
                    //             foreach(var wps in wpsOverviews)
                    //                 wps.Community = new CommunityOverview{
                    //                     Identifier = domain.Identifier,
                    //                     Title = domain.Name,
                    //                     Icon = domain.IconUrl,
                    //                     Status = domain.IsUserJoined(context.UserId) ? "joined" : ""
                    //                 };
                    //             wpssOverviews.AddRange(wpsOverviews);
                    //         }
                    //     } catch (Exception e) {
                    //         context.LogError(this, e != null ? e.Message : "Error while getting thematic applications of community " + domain.Name);
                    //     }
                    // }
                }
                context.LogDebug(this, string.Format("done"));
                
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new HttpResult(wpssOverviews);
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
                context.LogInfo(this,string.Format("/service/wps/{{wpsIdentifier}}/group GET WpsIdentifier='{0}'", request.WpsIdentifier));

                WpsProcessOffering wps = (WpsProcessOffering)WpsProcessOffering.FromIdentifier(context, request.WpsIdentifier);

                var gids = wps.GetAuthorizedGroupIds();
                List<int> ids = gids != null ? gids.ToList() : new List<int>();
                      
                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach (Group grp in groups) {
                    result.Add(new WebGroup(grp));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogInfo(this,string.Format("/service/wps/{{WpsIdentifier}}/group POST WpsIdentifier='{0}'", request.WpsIdentifier));
                WpsProcessOffering wps = (WpsProcessOffering)WpsProcessOffering.FromIdentifier(context, request.WpsIdentifier);

                var gids = wps.GetAuthorizedGroupIds();
                List<int> ids = gids != null ? gids.ToList() : new List<int>();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    if(grp.Id == request.Id) return new WebResponseBool(false);
                }

                wps.GrantPermissionsToGroups(new int[]{request.Id});

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogInfo(this,string.Format("/service/wps/{{WpsIdentifier}}/group/{{Id}} DELETE WpsIdentifier='{0}',Id='{1}'", request.WpsIdentifier, request.Id));

                var service = (WpsProcessOffering)Service.FromIdentifier(context, request.WpsIdentifier);
                service.RevokePermissionsFromGroup(request.Id);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps GET Identifier='{0}'", request.Identifier));

                WpsProcessOffering wps = (WpsProcessOffering)Service.FromIdentifier(context, request.Identifier);
                result = new WebServiceTep(context, wps);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Put the specified request.
        /// </summary>
        /// <returns>The put.</returns>
        /// <param name="request">Request.</param>
        public object Put(WpsServiceUpdateTagsRequestTep request){
			WebServiceTep result;

			var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			try {
				context.Open();
                context.AccessLevel = EntityAccessLevel.Privilege;
				context.LogInfo(this, string.Format("/service/wps/{{Id}} GET Id='{0}'", request.Id));
				WpsProcessOffering wps = (WpsProcessOffering)Service.FromIdentifier(context, request.Identifier);
                wps.AccessLevel = EntityAccessLevel.Privilege;
                wps.Tags = "";
                foreach (var tag in request.Tags) wps.AddTag(tag);
                wps.Store();
				result = new WebServiceTep(context, wps);
				context.Close();
			} catch (Exception e) {
				context.LogError(this, e.Message, e);
				context.Close();
				throw e;
			}
			return result;
        }

        public object Get(CurrentUserWpsServiceSyncRequestTep request){
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/user/current/service/wps/sync GET"));

                var user = UserTep.FromId(context, context.UserId);
                var domain = user.GetPrivateDomain();

                EntityList<WpsProvider> providers = new EntityList<WpsProvider>(context);
                providers.Load();

                foreach(var provider in providers){
                    if (provider.IsUserDeveloper(user.Id)) {
                        try {
                            provider.CanCache = false;
                            provider.UpdateProcessOfferings(true, user, domain, true);
                            context.WriteInfo(string.Format("CurrentUserWpsServiceSyncRequestTep -- synchro done for WPS {0}", provider.Name));
                        } catch (Exception e) {
                            context.WriteError(string.Format("CurrentUserWpsServiceSyncRequestTep -- {0} - {1}", e.Message, e.StackTrace));
                        }
                    }
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Get(WpsProviderSyncForUserRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/cr/wps/{{Identifier}}/sync GET, Identifier='{1}', Username='{0}'", request.Username, request.Identifier));

                var provider = (Terradue.Portal.WpsProvider)ComputingResource.FromIdentifier(context, request.Identifier);
                var user = UserTep.FromIdentifier(context, request.Username);
                var domain = user.GetPrivateDomain();

                if (!provider.IsUserDeveloper(user.Id)) throw new Exception("User is not set as developer for the provider " + provider.Name);

                try {
                    provider.CanCache = false;
                    provider.UpdateProcessOfferings(true, user, domain, true);
                    context.WriteInfo(string.Format("WpsProviderSyncForUserRequestTep -- synchro done for WPS {0} and user {1}", provider.Name, request.Username));
                } catch (Exception e) {
                    context.WriteError(string.Format("WpsProviderSyncForUserRequestTep -- {0} - {1}", e.Message, e.StackTrace));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Get(WpsServiceAllTagsRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            List<string> tags = new List<string>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/tags GET"));

                var alltags = context.GetQueryStringValues("SELECT tags FROM service;");

                foreach(var tag in alltags) {
                    var stags = tag.Split(",".ToCharArray());
                    foreach(var st in stags) {
                        if (!tags.Contains(st)) tags.Add(st);
                    }
                }
                tags.Sort();
                
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return tags;
        }

        public object Get(WpsServiceIconsRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            List<string> icons = new List<string>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/icons GET - remoteidentifier = {0}",request.SearchText));

                var sql = string.IsNullOrEmpty(request.SearchText)
                            ? "SELECT DISTINCT icon_url FROM service;"
                            : string.Format("SELECT DISTINCT icon_url FROM service WHERE (id IN (SELECT id FROM wpsproc WHERE remote_id LIKE '{0}') OR name like '{0}') AND icon_url IS NOT NULL AND icon_url <> '';", request.SearchText.Replace("*","%"));
                var iconsdb = context.GetQueryStringValues(sql);
                if (iconsdb != null) icons = iconsdb.ToList<String>();

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return icons;
        }

        public object Get(WpsServiceAllVersionsRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            List<string> versions = new List<string>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/versions GET"));

                var allversions = context.GetQueryStringValues("SELECT version FROM service;");

                foreach (var version in allversions) {
                    if (!versions.Contains(version))
                        versions.Add(version);
                }
                versions.Sort();

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return versions;
        }

        public object Put(WpsServiceUpdateAvailabilityRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{{identifier}}/available PUT, identifier='{0}'", request.Identifier));

                var wps = Service.FromIdentifier(context, request.Identifier);
                wps.Available = !wps.Available;
                wps.Store();
                
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Get(ReloadWpsForApp request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/wps/sync GET, app="+request.AppId));

                var appcached = ThematicApplicationCached.FromUid(context, request.AppId);                
                ThematicAppCachedFactory.SyncWpsServices(context, appcached);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Get(GetWPSServiceTokens request){            
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            var tokens = new List<WebWpsToken>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{0}/token GET",request.Identifier));

                EntityList<WpsToken> tokenList = new EntityList<WpsToken>(context);
                tokenList.SetFilter("ServiceId", request.Identifier);
                tokenList.Load();         
                foreach(var item in tokenList.GetItemsAsList()){
                    tokens.Add(new WebWpsToken(item));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return tokens;
        }

        public object Get(GetUserWpsTokens request){            
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            var tokens = new List<WebWpsToken>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/user/{0}/token GET",request.Username));

                var user = User.FromUsername(context, request.Username);

                EntityList<WpsToken> tokenList = new EntityList<WpsToken>(context);
                tokenList.SetFilter("OwnerId", user.Id);
                tokenList.Load();         
                foreach(var item in tokenList.GetItemsAsList()){
                    tokens.Add(new WebWpsToken(item, context));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return tokens;
        }        

        public object Get(GetCurrentUserWpsTokens request){            
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            var tokens = new List<WebWpsToken>();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/user/current/token GET"));                

                EntityList<WpsToken> tokenList = new EntityList<WpsToken>(context);
                tokenList.SetFilter("OwnerId", context.UserId);
                tokenList.Load();         
                foreach(var item in tokenList.GetItemsAsList()){
                    tokens.Add(new WebWpsToken(item, context));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return tokens;
        }        
                
        public object Post(PostWPSServiceToken request){            
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            var result = new WebWpsToken();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{0}/token POST",request.ServiceIdentifier));

                var token = request.ToEntity(context,null);
                token.Store();

                result = new WebWpsToken(token);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Put(PutWPSServiceToken request){            
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            var result = new WebWpsToken();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/service/wps/{0}/token PUT",request.ServiceIdentifier));

                var token = request.ToEntity(context,null);
                token.Store();

                result = new WebWpsToken(token);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        public object Delete(DeleteWPSServiceToken request){            
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            var result = new WebWpsToken();
            try {
                context.Open();
                context.LogInfo(this, string.Format("/wps/token/{0} DELETE",request.Id));

                var token = WpsToken.FromId(context, request.Id);
                token.Delete();

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }
    }

}

