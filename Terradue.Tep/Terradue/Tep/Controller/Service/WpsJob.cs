using System;
using Terradue.Portal;
using System.Collections.Generic;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using System.Collections.Specialized;
using Terradue.ServiceModel.Syndication;
using System.Xml;
using System.IO;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using System.Net;
using ServiceStack.Common.Web;
using OpenGis.Wps;
using System.Web;
using System.Runtime.Caching;

namespace Terradue.Tep {
    [EntityTable("wpsjob", EntityTableConfiguration.Custom, IdentifierField = "identifier", NameField = "name", HasOwnerReference = true, HasPrivilegeManagement = true)]
    /// <summary>
    /// A Wps Job is processed via a process installed on a wps. It takes as an entry a list of parameters.
    /// </summary>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    public class WpsJob : Entity, IAtomizable, IComparable<WpsJob> {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static MemoryCache ResultServletCache = new MemoryCache ("wpsjobResultServletCache");

        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("remote_identifier")]
        public string RemoteIdentifier { get; set; }

        [EntityDataField("wps")]
        public string WpsId { get; set; }

        [EntityDataField("process")]
        public string ProcessId { get; set; }

        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("status_url")]
        public string StatusLocation { get; set; }

        [EntityDataField("created_time")]
        public DateTime CreatedTime { get; set; }

        [EntityDataField("params")]
        public string parameters { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Terradue.Tep.WpsJob"/> can cache.
        /// </summary>
        /// <value><c>true</c> if can cache; otherwise, <c>false</c>.</value>
        public bool CanCache { get; set; }

        private CloudWpsFactory CloudWpsFactory;

        /// <summary>
        /// Gets or sets the parameters associated to the job
        /// </summary>
        /// <remarks>
        /// Paramaters are of type key/value
        /// </remarks>
        /// <value>The parameters.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public List<KeyValuePair<string, string>> Parameters { 
            get { 
                List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
                if (parameters != null)
                    result = (List<KeyValuePair<string, string>>)ServiceStack.Text.JsonSerializer.DeserializeFromString<List<KeyValuePair<string, string>>>(parameters);
                return result;
            } 
            set {
                parameters = ServiceStack.Text.JsonSerializer.SerializeToString<List<KeyValuePair<string, string>>>(value);
            } 
        }

        /// <summary>
        /// Get the Wps provider associated to the job
        /// </summary>
        /// <value>The provider.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        //public WpsProvider Provider {
        //    get {
        //        if (Process != null)
        //            return Process.Provider;
        //        else return null;
        //    }
        //}
        private WpsProvider provider { get; set; }
        public WpsProvider Provider {
            get {
                if (provider == null) {
                    try {
                        provider = (WpsProvider)WpsProvider.FromIdentifier (context, WpsId);
                    } catch (Exception) {
                        provider = null;    
                    }
                }
                return provider;
                if (Process != null) 
                    return Process.Provider;
                else return null;
            }
        }


        private WpsProcessOffering process { get; set; }

        /// <summary>
        /// Get the Wps process associated to the job
        /// </summary>
        /// <value>The process service.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public WpsProcessOffering Process {
            get { 
                if (process == null) {
                    if (String.IsNullOrEmpty (this.ProcessId)) return null;
                    try {
                        process = (WpsProcessOffering)WpsProcessOffering.FromIdentifier(context, ProcessId);
                    } catch (Exception e) {
                        string[] identifierParams = ProcessId.Split("-".ToCharArray());
                        if (identifierParams.Length == 3) {
                            switch (identifierParams[0]) {
                                case "one":
                                    try {
                                        if (this.IsPublic ()) CloudWpsFactory.StartDelegate (this.OwnerId);
                                        context.LogDebug (this, "Get process -- " + identifierParams [1] + " -- " + identifierParams [2]);
                                        process = CloudWpsFactory.CreateWpsProcessOfferingForOne(identifierParams[1], identifierParams[2]);
                                    } catch (Exception e2) {
                                        context.LogError (this, e2.Message);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        //if (process == null) throw e;
                    }
                }

                return process;
            }
        }

        private User owner { get; set; }
        public User Owner { 
            get{ 
                if(owner == null) owner = User.FromId(context, this.OwnerId);
                return owner;
            }
        }

        public WpsJob(IfyContext context) : base(context) {
            CloudWpsFactory = new CloudWpsFactory (context);
            CanCache = true;//default is true, to be set to false to disable the cache
        }

        public static WpsJob FromId(IfyContext context, int id) {
            WpsJob result = new WpsJob(context);
            result.Id = id;
            result.Load();
            return result;
        }

        public static WpsJob FromIdentifier(IfyContext context, string id) {
            WpsJob result = new WpsJob(context);
            result.Identifier = id;
            result.Load();
            return result;
        }

        public override void Store() {
            if (this.Id == 0) {
                this.CreatedTime = DateTime.UtcNow;
            }
            base.Store();
        }

        public bool IsPublic(){
            return HasGlobalPrivilege();
        }

        public bool IsPrivate(){
            return !IsPublic() && !IsRestricted();
        }

        public bool IsRestricted(){
			string sql = String.Format("SELECT COUNT(*) FROM wpsjob_priv WHERE id_wpsjob={0} AND ((id_usr IS NOT NULL AND id_usr != {1}) OR id_grp IS NOT NULL);", this.Id, this.OwnerId);
            return context.GetQueryIntegerValue(sql) > 0;
        }

        #region IAtomizable implementation

        public bool IsSearchable (NameValueCollection parameters) {
            string name = (this.Name != null ? this.Name : this.Identifier);
            string text = (this.TextContent != null ? this.TextContent : "");

            if (!string.IsNullOrEmpty (parameters ["q"])) {
                string q = parameters ["q"].ToLower ();
                if (!(name.ToLower ().Contains (q) || this.Identifier.ToLower ().Contains (q) || text.ToLower ().Contains (q)))
                    return false;
            }

            if (!string.IsNullOrEmpty (parameters ["public"]) && parameters ["public"].Equals ("true")) {
                if (this.IsPrivate ()) return false;
            }

            return true;
        }

        public AtomItem ToAtomItem(NameValueCollection parameters) {

            bool ispublic = this.IsPublic();

            string name = (this.Name != null ? this.Name : this.Identifier);
            string text = (this.TextContent != null ? this.TextContent : "");
            var entityType = EntityType.GetEntityType(typeof(WpsJob));
            Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);

            if (!IsSearchable (parameters)) return null;

            AtomItem result = new AtomItem();
            string statusloc = this.StatusLocation;

            if (Provider == null || Provider.Proxy) statusloc = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + this.Identifier;

            if (string.IsNullOrEmpty(parameters["basicrequest"]) || parameters["basicrequest"] != "true") {
                result = GetFullWpsJobAtomItem();
                if (result == null) {
                    result = new AtomItem();
                    ispublic = false;
                }
            }

            result.Id = id.ToString();
            result.Title = new TextSyndicationContent(name);
            result.Content = new TextSyndicationContent(name);

            result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            result.Summary = new TextSyndicationContent(name);
            result.ReferenceData = this;

            //TODO: temporary until https://git.terradue.com/sugar/terradue-portal/issues/15 is solved
            result.PublishDate = new DateTimeOffset(DateTime.SpecifyKind(this.CreatedTime, DateTimeKind.Utc));
//            result.PublishDate = new DateTimeOffset(this.CreatedTime);

            var basepath = new UriBuilder(context.BaseUrl);
            basepath.Path = "user";
            string usrUri = basepath.Uri.AbsoluteUri + "/" + Owner.Username ;
            string usrName = (!String.IsNullOrEmpty(Owner.FirstName) && !String.IsNullOrEmpty(Owner.LastName) ? Owner.FirstName + " " + Owner.LastName : Owner.Username);
            SyndicationPerson author = new SyndicationPerson(Owner.Email, usrName, usrUri);
            author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", Owner.Username));
            result.Authors.Add(author);
            result.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));
            Uri share = new Uri(context.BaseUrl + "/share?url=" +id.AbsoluteUri);
            result.Links.Add(new SyndicationLink(share, "via", name, "application/atom+xml", 0));
            result.Links.Add(new SyndicationLink(new Uri(statusloc), "alternate", "statusLocation", "application/atom+xml", 0));

            result.Categories.Add(new SyndicationCategory("remote_identifier", null, this.RemoteIdentifier));
            result.Categories.Add(new SyndicationCategory("visibility", null, ispublic ? "public" : (IsRestricted() ? "restricted" : "private")));

            return result;
        }

        private AtomItem GetFullWpsJobAtomItem(){

            OwcOffering offering = new OwcOffering();
            List<OwcOperation> operations = new List<OwcOperation>();

            MemoryStream ms = new MemoryStream();
            XmlWriter writer = XmlWriter.Create(ms);
            XmlDocument doc = new XmlDocument();
            List<XmlNode> nodes = new List<XmlNode>();

            string providerUrl = null;
            string identifier = this.Identifier;

            if(Provider == null){
                string[] identifierParams = this.ProcessId.Split("-".ToCharArray());
                if (identifierParams.Length == 3) {
                    switch (identifierParams[0]) {
                        case "one":
                            CloudWpsFactory wpstep = new CloudWpsFactory(context);
                            if (this.IsPublic()) wpstep.StartDelegate(this.OwnerId);
                            try{
                                context.LogDebug (this, "Get process from one -- " + identifierParams [1] + " -- " + identifierParams [2]);
                                process = wpstep.CreateWpsProcessOfferingForOne(identifierParams[1], identifierParams[2]);
                                context.LogDebug (this, "Get provider from one");
                                Provider = process.Provider;
                            }catch(Exception e2){
                                context.LogError (this, e2.Message);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            if (Provider != null && Process != null) {
                if (Provider.Proxy) {
                    identifier = Process.Identifier;
                    providerUrl = context.BaseUrl + "/wps/WebProcessingService";
                } else {
                    identifier = Process.RemoteIdentifier;
                    providerUrl = Provider.BaseUrl;
                }
            } else return null;

            Uri capabilitiesUri = new Uri(providerUrl + "?service=WPS" +
                                              "&request=GetCapabilities");

            Uri describeUri = new Uri(providerUrl + "?service=WPS" +
                                          "&request=DescribeProcess" +
                                      "&version=" + Process.Version +
                                          "&identifier=" + identifier);
            Uri executeUri = new Uri(providerUrl + "?service=WPS" +
                                         "&request=Execute" +
                                     "&version=" + Process.Version +
                                         "&identifier=" + identifier);

            //getcapabilities
            OwcOperation operation = new OwcOperation {
                Method = "GET",
                Code = "GetCapabilities",
                Href = capabilitiesUri.AbsoluteUri
            };
            OpenGis.Wps.GetCapabilities getcapabilities = new OpenGis.Wps.GetCapabilities();

            new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.GetCapabilities)).Serialize(writer, getcapabilities);
            writer.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            doc.Load(ms);

            nodes = new List<XmlNode>();
            nodes.Add(doc.DocumentElement.CloneNode(true));

            operation.Request = new OwcContent();
            operation.Request.Type = "text/xml";
            operation.Request.Any = (XmlElement)nodes [0];
            operations.Add(operation);

            //describeProcess
            operation = new OwcOperation{ Method = "GET", Code = "DescribeProcess", Href = describeUri.AbsoluteUri };
            OpenGis.Wps.DescribeProcess describe = new OpenGis.Wps.DescribeProcess();

            ms = new MemoryStream();
            writer = XmlWriter.Create(ms);

            new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.DescribeProcess)).Serialize(writer, describe);
            writer.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            doc.Load(ms);

            nodes = new List<XmlNode>();
            nodes.Add(doc.DocumentElement.CloneNode(true));

            operation.Request = new OwcContent();
            operation.Request.Type = "text/xml";
            operation.Request.Any = (XmlElement)nodes [0];
            operations.Add(operation);

            //execute
            operation = new OwcOperation{ Method = "POST", Code = "Execute", Href = executeUri.AbsoluteUri };
            OpenGis.Wps.Execute execute = new OpenGis.Wps.Execute();
            execute.Identifier = new OpenGis.Wps.CodeType{ Value = identifier };
            execute.DataInputs = new List<OpenGis.Wps.InputType>();
            foreach (var param in this.Parameters) {
                OpenGis.Wps.InputType input = new OpenGis.Wps.InputType();
                input.Identifier = new OpenGis.Wps.CodeType{ Value = param.Key };
                input.Data = new OpenGis.Wps.DataType{ Item = new OpenGis.Wps.LiteralDataType{ Value = param.Value } };
                execute.DataInputs.Add(input);
            }

            ms = new MemoryStream();
            writer = XmlWriter.Create(ms);

            new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.Execute)).Serialize(writer, execute);
            writer.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            doc.Load(ms);

            nodes = new List<XmlNode>();
            nodes.Add(doc.DocumentElement.CloneNode(true));

            operation.Request = new OwcContent();
            operation.Request.Type = "text/xml";
            operation.Request.Any = (XmlElement)nodes [0];
            operations.Add(operation);

            offering.Operations = operations.ToArray();

            OwsContextAtomEntry entry = new OwsContextAtomEntry();

            //TODO: temporary until https://git.terradue.com/sugar/terradue-portal/issues/15 is solved
            entry.PublishDate = new DateTimeOffset(DateTime.SpecifyKind(this.CreatedTime, DateTimeKind.Utc));
//            entry.PublishDate = new DateTimeOffset(this.CreatedTime);

            entry.Publisher = Owner.Username;

            entry.Offerings = new List<OwcOffering>{ offering };
            entry.Categories.Add(new SyndicationCategory("WpsOffering"));

            entry.Content = new TextSyndicationContent("This job has been created using the service " + Process.Name);
            ms.Close ();

            var resultAtom = new AtomItem (entry);
            return resultAtom;
        }


        public NameValueCollection GetOpenSearchParameters() {
            return OpenSearchFactory.GetBaseOpenSearchParameter();
        }

        #endregion

        #region IComparable implementation

        public int CompareTo(WpsJob other) {
            if (other == null)
                return 1;
            else
                return this.CreatedTime.CompareTo(other.CreatedTime);
        }

        #endregion

        public object GetExecuteResponse () { 
            var cacheItem = ResultServletCache.GetCacheItem (StatusLocation);
            if (cacheItem != null && CanCache) {
                return (ExecuteResponse)cacheItem.Value;
            } else {
                return GetAndCacheExecuteResponse ();
            }
        }

        private object GetAndCacheExecuteResponse () {
            OpenGis.Wps.ExecuteResponse execResponse = null;
            HttpWebRequest executeHttpRequest;
            if (Provider != null)
                executeHttpRequest = Provider.CreateWebRequest (StatusLocation);
            else {
                NetworkCredential credentials = null;
                var urib = new UriBuilder (StatusLocation);
                if (!string.IsNullOrEmpty (urib.UserName) && !string.IsNullOrEmpty (urib.Password)) credentials = new NetworkCredential (urib.UserName, urib.Password);
                executeHttpRequest = WpsProvider.CreateWebRequest (StatusLocation, credentials, context.Username);
            }
            if (StatusLocation.Contains ("gpod.eo.esa.int")) {
                executeHttpRequest.Headers.Add ("X-UserID", context.GetConfigValue ("GpodWpsUser"));
            }

            using (var remoteWpsResponseStream = new MemoryStream ()) {
                context.LogDebug (this, string.Format (string.Format ("Status url = {0}", executeHttpRequest.RequestUri != null ? executeHttpRequest.RequestUri.AbsoluteUri : "")));

                try {
                    using (var remoteWpsResponse = (HttpWebResponse)executeHttpRequest.GetResponse ())
                    using (var remotestream = remoteWpsResponse.GetResponseStream ())
                        remotestream.CopyTo (remoteWpsResponseStream);
                    remoteWpsResponseStream.Seek (0, SeekOrigin.Begin);
                    execResponse = (OpenGis.Wps.ExecuteResponse)new System.Xml.Serialization.XmlSerializer (typeof (OpenGis.Wps.ExecuteResponse)).Deserialize (remoteWpsResponseStream);

                } catch (WebException we) {
                    context.LogError (this, string.Format (we.Message));
                    //PATCH, waiting for http://project.terradue.com/issues/13615 to be resolved
                    if (StatusLocation.Contains ("gpod.eo.esa.int")) {
                        using (var remotestream = ((HttpWebResponse)we.Response).GetResponseStream ()) remotestream.CopyTo (remoteWpsResponseStream);
                        remoteWpsResponseStream.Seek (0, SeekOrigin.Begin);
                        execResponse = (OpenGis.Wps.ExecuteResponse)new System.Xml.Serialization.XmlSerializer (typeof (OpenGis.Wps.ExecuteResponse)).Deserialize (remoteWpsResponseStream);
                    } else if (we.Response != null && we.Response is HttpWebResponse) {
                        return new HttpResult (we.Message, ((HttpWebResponse)we.Response).StatusCode);
                    } else {
                        return new HttpResult (we.Message, HttpStatusCode.BadGateway);
                    }
                } catch (Exception e) {
                    OpenGis.Wps.ExceptionReport exceptionReport = null;
                    remoteWpsResponseStream.Seek (0, SeekOrigin.Begin);
                    try {
                        exceptionReport = (OpenGis.Wps.ExceptionReport)new System.Xml.Serialization.XmlSerializer (typeof (OpenGis.Wps.ExceptionReport)).Deserialize (remoteWpsResponseStream);
                    } catch (Exception e2) { }
                    remoteWpsResponseStream.Seek (0, SeekOrigin.Begin);
                    string errormsg = null;
                    using (StreamReader reader = new StreamReader (remoteWpsResponseStream)) {
                        errormsg = reader.ReadToEnd ();
                    }
                    remoteWpsResponseStream.Close ();
                    context.LogError (this, errormsg);
                    if (exceptionReport != null && exceptionReport.Exception != null) return new HttpResult (exceptionReport.Exception [0].ExceptionText [0], HttpStatusCode.BadRequest);
                    else return new HttpResult (errormsg, HttpStatusCode.BadRequest);
                }
            }

            execResponse.statusLocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + Identifier;

            if (execResponse.ProcessOutputs != null) {
                foreach (var output in execResponse.ProcessOutputs) {
                    try {
                        if (output.Identifier != null && output.Identifier.Value != null && output.Identifier.Value.Equals ("result_metadata")) {
                            context.LogDebug (this, string.Format ("Case result_metadata"));
                            var item = new ComplexDataType ();
                            var reference = new OutputReferenceType ();
                            reference.mimeType = "application/opensearchdescription+xml";
                            reference.href = context.BaseUrl + "/proxy/wps/" + Identifier + "/description";
                            item.Reference = reference;
                            item.Any = null;
                            item.mimeType = "application/xml";
                            output.Identifier = new CodeType { Value = "result_osd" };
                            output.Item = new DataType ();
                            ((DataType)(output.Item)).Item = item;
                        } else if (output.Identifier != null && output.Identifier.Value != null && output.Identifier.Value.Equals ("result_osd")) {
                            if (output.Item is DataType && ((DataType)(output.Item)).Item != null) {
                                context.LogDebug (this, string.Format ("Case result_osd"));
                                var item = ((DataType)(output.Item)).Item as ComplexDataType;
                                var reference = item.Reference as OutputReferenceType;
                                reference.href = context.BaseUrl + "/proxy?url=" + HttpUtility.UrlEncode (reference.href);
                                item.Reference = reference;
                                ((DataType)(output.Item)).Item = item;
                            } else if (output.Item is OutputReferenceType) {
                                context.LogDebug (this, string.Format ("Case result_osd"));
                                var reference = output.Item as OutputReferenceType;
                                reference.href = context.BaseUrl + "/proxy?url=" + HttpUtility.UrlEncode (reference.href);
                                output.Item = reference;
                            }
                        } else {
                            if (output.Identifier != null && output.Identifier.Value != null) context.LogDebug (this, string.Format ("Case {0}", output.Identifier.Value));
                            if (output.Item is DataType && ((DataType)(output.Item)).Item != null) {
                                var item = ((DataType)(output.Item)).Item as ComplexDataType;
                                if (item.Any != null) {
                                    var reference = new OutputReferenceType ();
                                    reference.mimeType = "application/opensearchdescription+xml";
                                    reference.href = context.BaseUrl + "/proxy/wps/" + Identifier + "/description";
                                    item.Reference = reference;
                                    item.Any = null;
                                    item.mimeType = "application/xml";
                                    output.Identifier = new CodeType { Value = "result_osd" };
                                }
                            }
                        }
                    } catch (Exception e) {
                        context.LogError (this, e.Message);
                    }
                }
            }
            Uri uri = new Uri (execResponse.serviceInstance);
            execResponse.serviceInstance = context.BaseUrl + uri.PathAndQuery;
            ResultServletCache.Set (new CacheItem (StatusLocation, execResponse), new CacheItemPolicy () { AbsoluteExpiration = DateTimeOffset.Now.AddHours (12) });
            return execResponse;
        }
    }
}


