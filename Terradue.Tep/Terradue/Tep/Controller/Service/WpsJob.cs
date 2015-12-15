using System;
using Terradue.Portal;
using System.Collections.Generic;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using System.Collections.Specialized;
using Terradue.ServiceModel.Ogc.OwsContext;
using Terradue.ServiceModel.Syndication;
using System.Xml;
using System.IO;

namespace Terradue.Tep {
    [EntityTable("wpsjob", EntityTableConfiguration.Custom, IdentifierField = "identifier", NameField = "name", HasOwnerReference = true, HasPrivilegeManagement = true)]
    /// <summary>
    /// A Wps Job is processed via a process installed on a wps. It takes as an entry a list of parameters.
    /// </summary>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    public class WpsJob : Entity, IAtomizable, IComparable<WpsJob> {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const int SERVICE_TABLE = 1;

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
                if (provider == null)
                    provider = (WpsProvider)WpsProvider.FromIdentifier(context, WpsId);
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
                        string[] identifierParams = ProcessId.Split("-".ToCharArray());
                        if (identifierParams.Length == 3) {
                            switch (identifierParams[0]) {
                                case "one":
                                    process = new CloudWpsFactory(context).CreateWpsProcessOfferingForOne(identifierParams[1], identifierParams[2]);
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

        private User owner { get; set; }
        public User Owner { 
            get{ 
                if(owner == null) owner = User.FromId(context, this.OwnerId);
                return owner;
            }
        }

        public WpsJob(IfyContext context) : base(context) {
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

        public new AtomItem ToAtomItem(NameValueCollection parameters) {

            bool ispublic = this.IsPublic();

            string identifier = null;
            string name = (this.Name != null ? this.Name : this.Identifier);
            string description = null;
            string text = (this.TextContent != null ? this.TextContent : "");
            var entityType = EntityType.GetEntityType(typeof(WpsJob));
            Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);

            if (!string.IsNullOrEmpty(parameters["q"])) {
                string q = parameters["q"].ToLower();
                if (!(name.ToLower().Contains(q) || this.Identifier.ToLower().Contains(q) || text.ToLower().Contains(q)))
                    return null;
            }
                
            if (!string.IsNullOrEmpty(parameters["public"]) && parameters["public"].Equals("true")) {
                if (this.IsPrivate()) return null;
            }
                
            WpsProcessOffering process = null;
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

            if (string.IsNullOrEmpty(parameters["basicrequest"]) || parameters["basicrequest"] != "true") {
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
            result.PublishDate = this.CreatedTime;
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

            WpsProcessOffering process = null;
            WpsProvider provider = null;

            try{
                process = (WpsProcessOffering)WpsProcessOffering.FromIdentifier(context, this.ProcessId);
                provider = (WpsProvider)WpsProvider.FromIdentifier(context, this.WpsId);
            } catch (Exception e) {
                string[] identifierParams = this.ProcessId.Split("-".ToCharArray());
                if (identifierParams.Length == 3) {
                    switch (identifierParams[0]) {
                        case "one":
                            CloudWpsFactory wpstep = new CloudWpsFactory(context);
                            if (this.IsPublic()) wpstep.StartDelegate(this.OwnerId);
                            try{
                                process = wpstep.CreateWpsProcessOfferingForOne(identifierParams[1], identifierParams[2]);
                                provider = process.Provider;
                            }catch(Exception e2){
                                
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
            Terradue.ServiceModel.Ogc.OwsContext.OwcOperation operation = new OwcOperation {
                Method = "GET",
                Code = "GetCapabilities",
                Href = capabilitiesUri
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
            ((OwcContent)operation.Request).Any = nodes.ToArray();
            operations.Add(operation);

            //describeProcess
            operation = new OwcOperation{ Method = "GET", Code = "DescribeProcess", Href = describeUri };
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
            ((OwcContent)operation.Request).Any = nodes.ToArray();
            operations.Add(operation);

            //execute
            operation = new OwcOperation{ Method = "POST", Code = "Execute", Href = executeUri };
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
            ((OwcContent)operation.Request).Any = nodes.ToArray();
            operations.Add(operation);

            offering.Operations = operations.ToArray();

            OwsContextAtomEntry entry = new OwsContextAtomEntry();

            entry.PublishDate = new DateTimeOffset(this.CreatedTime);
            entry.Publisher = Owner.Username;

            entry.Offerings = new List<OwcOffering>{ offering };
            entry.Categories.Add(new SyndicationCategory("WpsOffering"));

            entry.Content = new TextSyndicationContent("This job has been created using the service " + process.Name);
            return new AtomItem(entry);
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
    }
}


