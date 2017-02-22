using System;
using Terradue.Portal;
using System.Collections.Generic;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using System.Collections.Specialized;
using Terradue.ServiceModel.Syndication;
using System.Xml;
using System.IO;
using System.Net;
using ServiceStack.Common.Web;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using OpenGis.Wps;
using System.Linq;
using Terradue.Tep.OpenSearch;
using Terradue.Portal.OpenSearch;
using System.Web;

namespace Terradue.Tep {
    [EntityTable("wpsjob", EntityTableConfiguration.Custom, IdentifierField = "identifier", NameField = "name", HasOwnerReference = true, HasPermissionManagement = true, HasDomainReference = true, AllowsKeywordSearch = true)]
    /// <summary>
    /// A Wps Job is processed via a process installed on a wps. It takes as an entry a list of parameters.
    /// </summary>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    public class WpsJob : EntitySearchable, IComparable<WpsJob> {


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

        private WpsProvider provider { get; set; }

        /// <summary>
        /// Get the Wps provider associated to the job
        /// </summary>
        /// <value>The provider.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public WpsProvider Provider {
            get {
                if (provider == null) {
                    try {
                        provider = (WpsProvider)WpsProvider.FromIdentifier(context, WpsId);
                    } catch (Exception) {
                        provider = null;
                    }
                }
                return provider;
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
                    try {
                        process = (WpsProcessOffering)WpsProcessOffering.FromIdentifier(context, ProcessId);
                    } catch (Exception e) {
                        string [] identifierParams = ProcessId.Split("-".ToCharArray());
                        if (identifierParams.Length == 3) {
                            switch (identifierParams [0]) {
                            case "one":
                                process = new CloudWpsFactory(context).CreateWpsProcessOfferingForOne(identifierParams [1], identifierParams [2]);
                                break;
                            default:
                                break;
                            }
                        }
                        if (process == null) throw e;
                    }
                }

                return process;
            }
        }

        private UserTep owner;
        public UserTep Owner {
            get {
                if (owner == null) {
                    if (OwnerId != 0) owner = UserTep.FromId(context, OwnerId);
                }
                return owner;
            }
        }

        /// <summary>
        /// Gets the total results.
        /// </summary>
        /// <value>The total results.</value>
        public long TotalResults {
            get {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the default type of the MIME.
        /// </summary>
        /// <value>The default type of the MIME.</value>
        public string DefaultMimeType {
            get {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Terradue.Tep.WpsJob"/> can cache.
        /// </summary>
        /// <value><c>true</c> if can cache; otherwise, <c>false</c>.</value>
        public bool CanCache {
            get {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WpsJob"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public WpsJob(IfyContext context) : base(context) { }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static WpsJob FromId(IfyContext context, int id) {
            WpsJob result = new WpsJob(context);
            result.Id = id;
            result.Load();
            return result;
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static WpsJob FromIdentifier(IfyContext context, string id) {
            WpsJob result = new WpsJob(context);
            result.Identifier = id;
            result.Load();
            return result;
        }

        /// <summary>
        /// Store this instance.
        /// </summary>
        public override void Store() {
            if (DomainId == 0) DomainId = Owner.Domain.Id;
            if (this.Id == 0) {
                this.CreatedTime = DateTime.UtcNow;
            }
            base.Store();
        }

        /// <summary>
        /// Is the job public.
        /// </summary>
        /// <returns><c>true</c>, if public was ised, <c>false</c> otherwise.</returns>
        public bool IsPublic() {
            return DoesGrantPermissionsToAll();
        }

        /// <summary>
        /// Is the job private.
        /// </summary>
        /// <returns><c>true</c>, if private was ised, <c>false</c> otherwise.</returns>
        public bool IsPrivate() {
            return !IsPublic() && !IsRestricted();
        }

        /// <summary>
        /// Is the job shared to community.
        /// </summary>
        /// <returns><c>true</c>, if shared to community, <c>false</c> otherwise.</returns>
        public bool IsSharedToCommunity() {
            return (this.Owner != null && this.DomainId != this.Owner.DomainId);
        }

        /// <summary>
        /// Is the job shared to user.
        /// </summary>
        /// <returns><c>true</c>, if shared to community, <c>false</c> otherwise.</returns>
        public bool IsSharedToUser() {
            var sharedUsersIds = this.GetUsersWithPermissions();
            return sharedUsersIds != null && (sharedUsersIds.Count > 1 || !sharedUsersIds.Contains(this.Owner.Id));
        }

        /// <summary>
        /// Is the job shared to user.
        /// </summary>
        /// <returns><c>true</c>, if shared to community, <c>false</c> otherwise.</returns>
        /// <param name="id">Identifier.</param>
        public bool IsSharedToUser(int id) { 
            var sharedUsersIds = this.GetUsersWithPermissions();
            return sharedUsersIds != null && (sharedUsersIds.Contains(id));
        }

        /// <summary>
        /// Is the job restricted.
        /// </summary>
        /// <returns><c>true</c>, if restricted was ised, <c>false</c> otherwise.</returns>
        public bool IsRestricted() {
            string sql = String.Format("SELECT COUNT(*) FROM wpsjob_perm WHERE id_wpsjob={0} AND ((id_usr IS NOT NULL AND id_usr != {1}) OR id_grp IS NOT NULL);", this.Id, this.OwnerId);
            return context.GetQueryIntegerValue(sql) > 0;
        }

        public NetworkCredential GetCredentials() {
            var urib = new UriBuilder(StatusLocation);
            if (!string.IsNullOrEmpty(urib.UserName) && !string.IsNullOrEmpty(urib.Password))
                return new NetworkCredential(urib.UserName, urib.Password);
            return null;

        }

        /// <summary>
        /// Gets the execute response.
        /// </summary>
        /// <returns>The execute response.</returns>
        public object GetStatusLocationContent() {
            //Create Web request
            HttpWebRequest executeHttpRequest;
            if (Provider != null)
                executeHttpRequest = Provider.CreateWebRequest(StatusLocation);
            else {
                // if credentials in the status URL
                NetworkCredential credentials = GetCredentials();
                executeHttpRequest = WpsProvider.CreateWebRequest(StatusLocation, credentials, context.Username);
                if (credentials != null)
                    executeHttpRequest.PreAuthenticate = true;
            }

            // G-POD case: identified with HTTP header as GpodWpsUser
            if (StatusLocation.Contains("gpod.eo.esa.int")) {
                executeHttpRequest.Headers.Add("X-UserID", context.GetConfigValue("GpodWpsUser"));
            }

            //create response
            OpenGis.Wps.ExecuteResponse execResponse = null;

            using (var remoteWpsResponseStream = new MemoryStream()) {
                context.LogDebug(this, string.Format(string.Format("Status url = {0}", executeHttpRequest.RequestUri != null ? executeHttpRequest.RequestUri.AbsoluteUri : "")));

                // HTTP request
                try {
                    using (var remoteWpsResponse = (HttpWebResponse)executeHttpRequest.GetResponse()) {
                        using (var remotestream = remoteWpsResponse.GetResponseStream()) {
                            remotestream.CopyTo(remoteWpsResponseStream);
                        }
                    }

                } catch (WebException we) {
                    context.LogError(this, string.Format(we.Message));

                    //PATCH, waiting for http://project.terradue.com/issues/13615 to be resolved
                    if (StatusLocation.Contains("gpod.eo.esa.int")) {
                        using (var remotestream = ((HttpWebResponse)we.Response).GetResponseStream()) remotestream.CopyTo(remoteWpsResponseStream);
                        remoteWpsResponseStream.Seek(0, SeekOrigin.Begin);
                        execResponse = (OpenGis.Wps.ExecuteResponse)WpsFactory.ExecuteResponseSerializer.Deserialize(remoteWpsResponseStream);
                    }

                    throw new WpsProxyException("Error proxying Status location " + StatusLocation, we);

                }

                // Deserialization
                try {
                    remoteWpsResponseStream.Seek(0, SeekOrigin.Begin);
                    execResponse = (OpenGis.Wps.ExecuteResponse)WpsFactory.ExecuteResponseSerializer.Deserialize(remoteWpsResponseStream);
                    return execResponse;
                } catch (Exception e) {
                    // Maybe an exceptionReport
                    OpenGis.Wps.ExceptionReport exceptionReport = null;
                    remoteWpsResponseStream.Seek(0, SeekOrigin.Begin);
                    try {
                        exceptionReport = (OpenGis.Wps.ExceptionReport)WpsFactory.ExceptionReportSerializer.Deserialize(remoteWpsResponseStream);
                        return exceptionReport;
                    } catch (Exception e2) {
                        remoteWpsResponseStream.Seek(0, SeekOrigin.Begin);
                        string errormsg = null;
                        using (StreamReader reader = new StreamReader(remoteWpsResponseStream)) {
                            errormsg = reader.ReadToEnd();
                        }
                        remoteWpsResponseStream.Close();
                        context.LogError(this, errormsg);
                        return errormsg;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the result URL from execute response.
        /// </summary>
        /// <returns>The result URL from execute response.</returns>
        /// <param name="execResponse">Exec response.</param>
        public IOpenSearchable GetProductOpenSearchable() {
            var content = GetStatusLocationContent();

            if (content is ExceptionReport)
                throw new ImpossibleSearchException("WPS job status raised an exception : "
                                                    + (content as ExceptionReport).Exception [0].ExceptionText [0]);

            if (!(content is ExecuteResponse))
                throw new ImpossibleSearchException("WPS job status did not return an ExecuteResponse : "
                                                    + content.ToString());

            ExecuteResponse execResponse = content as ExecuteResponse;

            //Go through results
            if (execResponse.ProcessOutputs == null || execResponse.ProcessOutputs.Count == 0)
                return new AtomFeedOpenSearchable(new AtomFeed());

            // Search for an Opensearch Description Document ouput url
            var result_osd = execResponse.ProcessOutputs.Where(po => po.Identifier.Value.Equals("result_osd"));
            if (result_osd.Count() > 0) {
                var po = result_osd.First();
                string url = null;
                //Get result Url
                if (po.Item is DataType && ((DataType)(po.Item)).Item != null) {
                    var item = ((DataType)(po.Item)).Item as ComplexDataType;
                    var reference = item.Reference as OutputReferenceType;
                    url = reference.href;
                } else if (po.Item is OutputReferenceType) {
                    var reference = po.Item as OutputReferenceType;
                    url = reference.href;
                }
                if (string.IsNullOrEmpty(url))
                    throw new ImpossibleSearchException("Ouput result_osd found but no Url set");


                OpenSearchUrl osUrl = null;
                try {
                    osUrl = new OpenSearchUrl(url);
                } catch (Exception) {
                    throw new ImpossibleSearchException("Ouput result_osd found invalid url : " + url);
                }

                return SandboxOpenSearchable.CreateSandboxOpenSearchable(osUrl, MasterCatalogue.OpenSearchEngine);
            }

            // Search for a static metadata file
            result_osd = execResponse.ProcessOutputs.Where(po => po.Identifier.Value.Equals("result_metadata"));
            if (result_osd.Count() > 0) {
                var po = result_osd.First();
                string url = null;
                //Get result Url
                if (po.Item is DataType && ((DataType)(po.Item)).Item != null) {
                    var item = ((DataType)(po.Item)).Item as ComplexDataType;
                    var reference = item.Reference as OutputReferenceType;
                    url = reference.href;
                } else if (po.Item is OutputReferenceType) {
                    var reference = po.Item as OutputReferenceType;
                    url = reference.href;
                }
                if (string.IsNullOrEmpty(url))
                    throw new ImpossibleSearchException("Ouput result_metadata found but no Url set");
                AtomFeed feed = null;
                try {
                    HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                    httpRequest.Credentials = GetCredentials();
                    if (httpRequest.Credentials != null)
                        httpRequest.PreAuthenticate = true;
                    using (var httpResp = httpRequest.GetResponse()) {
                        feed = AtomFeed.Load(XmlReader.Create(httpResp.GetResponseStream()));
                        feed.Id = url;
                    }
                } catch (Exception e) {
                    throw new ImpossibleSearchException("Ouput result_metadata found but impossible to load url : " + url + e.Message);
                }
                return new AtomFeedOpenSearchable(feed);
            }

            return new ExecuteResponseOutputOpenSearchable(execResponse, context);
        }

        #region IEntitySearchable implementation
        public override KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
                case "correlatedTo":
                var entity = new UrlBasedOpenSearchable(context, new OpenSearchUrl(value), MasterCatalogue.OpenSearchEngine).Entity;
                if (entity is EntityList<ThematicCommunity>) {
                    var entitylist = entity as EntityList<ThematicCommunity>;
                    var items = entitylist.GetItemsAsList();
                    if (items.Count > 0) {
                        return new KeyValuePair<string, string>("DomainId", items[0].Id.ToString());
                    }
                }
                return new KeyValuePair<string, string>();
            default:
                return base.GetFilterForParameter(parameter, value);
            }
        }

        #endregion



        #region IAtomizable implementation

        public new NameValueCollection GetOpenSearchParameters() {
            NameValueCollection nvc = base.GetOpenSearchParameters();
            nvc.Add("basic", "{t2:basic?}");
            return nvc;
        }

        public override bool IsPostFiltered(NameValueCollection parameters) {
            foreach (var key in parameters.AllKeys) {
                switch (parameters[key]) {
                default:
                    return false;
                }
            }
            return false;
        }

        public override AtomItem ToAtomItem(NameValueCollection parameters) {

            bool ispublic = this.IsPublic();
            var status = ispublic ? "public" : "private";

            string identifier = null;
            string name = (this.Name != null ? this.Name : this.Identifier);
            string text = (this.TextContent != null ? this.TextContent : "");
            var entityType = EntityType.GetEntityType(typeof(WpsJob));
            Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);

            WpsProvider provider = null;
            AtomItem result = new AtomItem();
            string statusloc = this.StatusLocation;

            try {
                provider = (WpsProvider)WpsProvider.FromIdentifier(context, this.WpsId);

                if (provider.Proxy) statusloc = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + this.Identifier;

            } catch (Exception e) {
                //if provider not on db, then it is proxied
                statusloc = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + this.Identifier;
            }

            if (string.IsNullOrEmpty(parameters ["basicrequest"]) || parameters ["basicrequest"] != "true") {
                result = GetFullWpsJobAtomItem();
                if (result == null) {
                    result = new AtomItem();
                    ispublic = false;
                }
            }

            result.Id = id.ToString();
            result.Title = new TextSyndicationContent(identifier);
            result.Content = new TextSyndicationContent(name);

            result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            result.Summary = new TextSyndicationContent(name);
            result.ReferenceData = this;

            result.PublishDate = new DateTimeOffset(this.CreatedTime);

            var basepath = new UriBuilder(context.BaseUrl);
            basepath.Path = "user";
            string usrUri = basepath.Uri.AbsoluteUri + "/" + Owner.Username;
            string usrName = (!String.IsNullOrEmpty(Owner.FirstName) && !String.IsNullOrEmpty(Owner.LastName) ? Owner.FirstName + " " + Owner.LastName : Owner.Username);
            SyndicationPerson author = new SyndicationPerson(Owner.Email, usrName, usrUri);
            author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", Owner.Username));
            result.Authors.Add(author);
            result.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));
            Uri share = new Uri(context.BaseUrl + "/share?url=" + id.AbsoluteUri);
            result.Links.Add(new SyndicationLink(share, "via", name, "application/atom+xml", 0));
            result.Links.Add(new SyndicationLink(new Uri(statusloc), "alternate", "statusLocation", "application/atom+xml", 0));
            if (Owner.Id == context.UserId) {
                //for owner only, we give the link to know with who the wpsjob is shared
                //if shared with users
                if (IsSharedToUser()) {
                    Uri sharedUrlUsr = new Uri(string.Format("{0}/user/search?correlatedTo={1}", context.BaseUrl, HttpUtility.UrlEncode(id.AbsoluteUri)));
                    result.Links.Add(new SyndicationLink(sharedUrlUsr, "results", name, "application/atom+xml", 0));
                    status = "restricted";
                }
                if (IsSharedToCommunity()) {
                    Uri sharedUrlCommunity = new Uri(string.Format("{0}/community/search?correlatedTo={1}", context.BaseUrl, HttpUtility.UrlEncode(id.AbsoluteUri)));
                    result.Links.Add(new SyndicationLink(sharedUrlCommunity, "results", name, "application/atom+xml", 0));
                    status = "restricted";
                }
            }

            result.Categories.Add(new SyndicationCategory("remote_identifier", null, this.RemoteIdentifier));
            result.Categories.Add(new SyndicationCategory("visibility", null, status));

            return result;
        }

        private AtomItem GetFullWpsJobAtomItem() {

            OwcOffering offering = new OwcOffering();
            List<OwcOperation> operations = new List<OwcOperation>();

            MemoryStream ms = new MemoryStream();
            XmlWriter writer = XmlWriter.Create(ms);
            XmlDocument doc = new XmlDocument();
            List<XmlNode> nodes = new List<XmlNode>();

            string providerUrl = null;
            string identifier = this.Identifier;

            WpsProcessOffering process = null;
            WpsProvider provider = null;

            try {
                process = (WpsProcessOffering)WpsProcessOffering.FromIdentifier(context, this.ProcessId);
                provider = (WpsProvider)WpsProvider.FromIdentifier(context, this.WpsId);
            } catch (Exception e) {
                context.LogError(this, e.Message);
                string [] identifierParams = this.ProcessId.Split("-".ToCharArray());
                if (identifierParams.Length == 3) {
                    switch (identifierParams [0]) {
                    case "one":
                        CloudWpsFactory wpstep = new CloudWpsFactory(context);
                        if (this.IsPublic()) wpstep.StartDelegate(this.OwnerId);
                        try {
                            context.LogDebug(this, "Get process -- " + identifierParams [1] + " -- " + identifierParams [2]);
                            process = wpstep.CreateWpsProcessOfferingForOne(identifierParams [1], identifierParams [2]);
                            context.LogDebug(this, "Get provider");
                            provider = process.Provider;
                        } catch (Exception e2) {
                            context.LogError(this, e2.Message);
                        }
                        break;
                    default:
                        break;
                    }
                }
            }

            if (provider != null && process != null) {
                if (provider.Proxy) {
                    identifier = process.Identifier;
                    providerUrl = context.BaseUrl + "/wps/WebProcessingService";
                } else {
                    identifier = process.RemoteIdentifier;
                    providerUrl = provider.BaseUrl;
                }
            }


            if (process == null || provider == null)
                return null;

            if (provider.Proxy) {
                identifier = process.Identifier;
                providerUrl = context.BaseUrl + "/wps/WebProcessingService";
            } else {
                identifier = process.RemoteIdentifier;
                providerUrl = provider.BaseUrl;
            }

            Uri capabilitiesUri = new Uri(providerUrl + "?service=WPS" +
                                              "&request=GetCapabilities");

            Uri describeUri = new Uri(providerUrl + "?service=WPS" +
                                          "&request=DescribeProcess" +
                                      "&version=" + process.Version +
                                          "&identifier=" + identifier);
            Uri executeUri = new Uri(providerUrl + "?service=WPS" +
                                         "&request=Execute" +
                                     "&version=" + process.Version +
                                         "&identifier=" + identifier);

            //getcapabilities
            var operation = new OwcOperation {
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
            operation = new OwcOperation { Method = "GET", Code = "DescribeProcess", Href = describeUri.AbsoluteUri };
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
            operation = new OwcOperation { Method = "POST", Code = "Execute", Href = executeUri.AbsoluteUri };
            OpenGis.Wps.Execute execute = new OpenGis.Wps.Execute();
            execute.Identifier = new OpenGis.Wps.CodeType { Value = identifier };
            execute.DataInputs = new List<OpenGis.Wps.InputType>();
            foreach (var param in this.Parameters) {
                OpenGis.Wps.InputType input = new OpenGis.Wps.InputType();
                input.Identifier = new OpenGis.Wps.CodeType { Value = param.Key };
                input.Data = new OpenGis.Wps.DataType { Item = new OpenGis.Wps.LiteralDataType { Value = param.Value } };
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

            entry.PublishDate = new DateTimeOffset(this.CreatedTime);

            entry.Publisher = Owner.Username;

            entry.Offerings = new List<OwcOffering> { offering };
            entry.Categories.Add(new SyndicationCategory("WpsOffering"));

            entry.Content = new TextSyndicationContent("This job has been created using the service " + process.Name);
            return new AtomItem(entry);
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

    }
}


