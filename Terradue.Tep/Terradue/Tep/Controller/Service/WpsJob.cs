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
using System.Runtime.Serialization;
using Terradue.OpenSearch.Schema;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Request;

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

        [EntityDataField("wps_version")]
        public string WpsVersion { get; set; }

        [EntityDataField("process")]
        public string ProcessId { get; set; }

        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("status_url")]
        public string StatusLocation { get; set; }

        [EntityDataField("ows_url")]
        public string OwsUrl { get; set; }

        [EntityDataField("status")]
        public WpsJobStatus Status { get; set; }

        [EntityDataField("created_time")]
        public DateTime CreatedTime { get; set; }

        [EntityDataField("end_time")]
        public DateTime EndTime { get; set; }

        [EntityDataField("nbresults")]
        public int NbResults { get; set; }

        [EntityDataField("params")]
        public string parameters { get; protected set; }

        [EntityDataField("app_identifier")]
        public string AppIdentifier { get; set; }

        [EntityDataField("access_key")]
        public string accesskey { get; protected set; }
        public string AccessKey {
            get {
                if (accesskey == null) {
                    var accesslevel = context.AccessLevel;
                    context.AccessLevel = EntityAccessLevel.Administrator;
                    accesskey = Guid.NewGuid().ToString();
                    var tmpjob = WpsJob.FromId(context, this.Id);
                    tmpjob.AccessKey = accesskey;
                    tmpjob.Store();
                    context.AccessLevel = accesslevel;
                }
                return accesskey;
            }
            protected set {
                accesskey = value;
            }
        }

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
                        string[] identifierParams = WpsId.Split("-".ToCharArray());
                        if (identifierParams.Length == 3) {
                            switch (identifierParams[0]) {
                                case "one":
                                    provider = new CloudWpsFactory(context).CreateWpsProviderForOne(identifierParams[1]);
                                    break;
                                default:
                                    break;
                            }
                        }
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
                return 0;
            }
        }

        /// <summary>
        /// Gets the default type of the MIME.
        /// </summary>
        /// <value>The default type of the MIME.</value>
        public string DefaultMimeType {
            get {
                return "application/atom+xml";
            }
        }

        /// <summary>
        /// Publish the job to the catalogue index
        /// </summary>
        /// <param name="index">Index.</param>
        /// <param name="username">Username used to publish into the catalogue index</param>
        /// <param name="apikey">Apikey used to publish into the catalogue index</param>
        /// <param name="appId">App identifier.</param>
        public void PublishToIndex(string index, string username, string apikey, string appId) {
            try {
                if (string.IsNullOrEmpty(index)) throw new Exception("Catalog index not set");
                var feed = GetJobAtomFeedFromOwsUrl(appId);

                if (feed != null) {
                    var entry = feed.Items.First();
                    var resultLink = entry.Links.FirstOrDefault(l => l.RelationshipType == "results");
                    feed = UpdateResultLinksForPublish(feed, index);
                    var user = UserTep.FromId(context, context.UserId);

                    //publish job feed as entry
                    CatalogueFactory.PostAtomFeedToIndex(context, feed, index, username, apikey);

                    //publish job results as entries
                    if (resultLink != null){
                        var resultFeed = GetJobResultsAtomFeedFromLink(resultLink);
                        var updatedResultItems = new List<OwsContextAtomEntry>();
                        var parentSelfB = new UriBuilder(string.Format("{0}/{1}/search?uid={2}", context.GetConfigValue("catalog-baseurl"), index, this.Identifier));
                        foreach (var item in resultFeed.Items){
                            try {
                                item.Categories.Add(new SyndicationCategory("jobresult", "http://www.terradue.com/api/type", "jobresult"));
                                item.Categories.Add(new SyndicationCategory(this.Identifier.Replace("-", ""), "http://www.terradue.com/api/jobid", this.Identifier));
                                var link = new SyndicationLink(parentSelfB.Uri, "results", "Originator job", "application/atom+xml", 0);
                                link.AttributeExtensions.Add(new System.Xml.XmlQualifiedName("level"), "info");
                                item.Links.Add(link);
                                updatedResultItems.Add(item);
                            }catch(Exception e){
                                context.LogError(this, "Unable to publish job result on catalog index : " + e.Message);
                            }
                        }
                        resultFeed.Items = updatedResultItems;
                        CatalogueFactory.PostAtomFeedToIndex(context, resultFeed, index, username, apikey);
                    } else {
                        context.LogError(this, "Unable to publish job results on catalog index : no results link");
                    }
                } else {
                    context.LogError(this, "Unable to publish on catalog index : feed is empty");
                }
            } catch (Exception e) {
                context.LogError(this, "Unable to publish on catalog index : " + e.Message);
            }
        }

        /// <summary>
        /// Unpublish job from the index of the catalogue.
        /// </summary>
        /// <param name="index">Index.</param>
        public void UnPublishFromIndex(string index, string username, string apikey) {
            var identifier = this.Identifier;
            try {
                var user = UserTep.FromId(context, context.UserId);

                //remove entry from catalogue index
                CatalogueFactory.DeleteEntryFromIndex(context, index, this.Identifier, username, apikey);

                //remove associated results
                identifier = null;
                var urlb = new UriBuilder(string.Format("{0}/{1}/search?cat={2}", context.GetConfigValue("catalog-baseurl"), index, this.Identifier.Replace("-", "")));
                OwsContextAtomFeed feed = null;
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(urlb.Uri.AbsoluteUri);
                using (var resp = httpRequest.GetResponse()) {
                    using (var stream = resp.GetResponseStream()) {
                        feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(stream);
                    }
                }
                if (feed != null) {
                    foreach (var item in feed.Items) {
                        identifier = ThematicAppCachedFactory.GetIdentifierFromFeed(item);
                        CatalogueFactory.DeleteEntryFromIndex(context, index, identifier, username, apikey);
                    }
                }
            } catch (Exception e) {
                context.LogError(this, "Unable to unpublish from catalog community index (" + identifier + ") : " + e.Message);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Terradue.Tep.WpsJob"/> can cache.
        /// </summary>
        /// <value><c>true</c> if can cache; otherwise, <c>false</c>.</value>
        public bool CanCache {
            get {
                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WpsJob"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public WpsJob(IfyContext context) : base(context) {}

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

        public override void Load() {
            base.Load();
        }

        public static WpsJob Copy(WpsJob job, IfyContext context) {
            WpsJob newjob = new WpsJob(context);
            newjob.OwnerId = context.UserId;
            newjob.UserId = context.UserId;
            newjob.Identifier = Guid.NewGuid().ToString();
            newjob.StatusLocation = job.StatusLocation;
            newjob.OwsUrl = job.OwsUrl;
            newjob.AppIdentifier = job.AppIdentifier;
            newjob.Status = job.Status;
            newjob.Parameters = job.Parameters;
            newjob.EndTime = job.EndTime;
            newjob.Name = job.Name;
            newjob.ProcessId = job.ProcessId;
            newjob.RemoteIdentifier = job.RemoteIdentifier;
            newjob.WpsId = job.WpsId;
            newjob.WpsVersion = job.WpsVersion;
            newjob.Store();

            newjob.CreatedTime = job.CreatedTime;
            newjob.NbResults = job.NbResults;
            newjob.Store();

            return newjob;
        }

        /// <summary>
        /// Store this instance.
        /// </summary>
        public override void Store() {
            if (DomainId == 0) DomainId = Owner.Domain.Id;
            bool newjob = false;
            if (this.Id == 0) {
                newjob = true;
                this.CreatedTime = DateTime.UtcNow;
                this.AccessKey = Guid.NewGuid().ToString();
                this.NbResults = -1;
                try {
                    if (string.IsNullOrEmpty(this.WpsVersion) && this.Process != null) this.WpsVersion = this.Process.Version;//we set only at creation as service version may change with time
                }catch(Exception e){
                    //if error while getting Process, we skip the version
                }
            }
            base.Store();
            if (newjob && context.AccessLevel == EntityAccessLevel.Administrator) {
                var count = context.GetQueryIntegerValue(String.Format("SELECT count(*) FROM {3} WHERE id_{2}={0} AND id_usr={1};", Id, OwnerId, this.EntityType.PermissionSubjectTable.Name, this.EntityType.PermissionSubjectTable.PermissionTable));
                if (count == 0)
                    context.Execute(String.Format("INSERT INTO {3} (id_{2}, id_usr) VALUES ({0}, {1});", Id, OwnerId, this.EntityType.PermissionSubjectTable.Name, this.EntityType.PermissionSubjectTable.PermissionTable));
            }
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
		/// <returns><c>true</c>, if shared to user, <c>false</c> otherwise.</returns>
        public bool IsSharedToUser() {
            var sharedUsersIds = this.GetAuthorizedUserIds();
            return sharedUsersIds != null && (sharedUsersIds.Length > 1 || !sharedUsersIds.Contains(this.Owner.Id));
        }

        /// <summary>
        /// Is the job shared to user.
        /// </summary>
		/// <returns><c>true</c>, if shared to user, <c>false</c> otherwise.</returns>
        /// <param name="id">Identifier.</param>
		/// <param name="policy">Policy of sharing (direct = permission directly given to the user, role = permission only given via role and privilege, none = one of both previous cases ).</param>
		public bool IsSharedToUser(int id, string policy = "none") {
			bool permissionOnly = false;
			bool privilegeOnly = false;
			switch(policy){
				case "permission":
				    permissionOnly = true;
				    break;
				case "privilege":
				    privilegeOnly = true;
                    break;
				default:
					break;
			}
			var sharedUsersIds = this.GetAuthorizedUserIds(permissionOnly, privilegeOnly);
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
        /// Creates the job from execute input.
        /// </summary>
        /// <returns>The job from execute input.</returns>
        /// <param name="context">Context.</param>
        /// <param name="wps">Wps.</param>
        /// <param name="executeInput">Execute input.</param>
        public static WpsJob CreateJobFromExecuteInput(IfyContext context, WpsProcessOffering wps, Execute executeInput, List<KeyValuePair<string, string>> parameters) {
            WpsJob wpsjob = new WpsJob(context);
            context.LogDebug(wpsjob, string.Format("Creating job from execute request"));
            string newId = Guid.NewGuid().ToString();

            //create WpsJob
            context.LogDebug(wpsjob, string.Format("Provider is null ? -> " + (wps.Provider == null ? "true" : "false")));

            wpsjob.Name = wps.Name;
            wpsjob.Identifier = newId;
            wpsjob.OwnerId = context.UserId;
            wpsjob.UserId = context.UserId;
            wpsjob.WpsId = wps.Provider.Identifier;
            wpsjob.ProcessId = wps.Identifier;
            wpsjob.CreatedTime = DateTime.UtcNow;
            wpsjob.Status = WpsJobStatus.NONE;
            wpsjob.Parameters = new List<KeyValuePair<string, string>>();
            wpsjob.Parameters = parameters;
            wpsjob.WpsVersion = wps.Version;

            if (executeInput != null && executeInput.DataInputs != null) {
                var tmpInputs = new List<InputType>();
                foreach (var input in executeInput.DataInputs) {
                    switch (input.Identifier.Value) {
                        case "_T2InternalJobTitle":
                            var item = input.Data.Item;
                            var ld = item as LiteralDataType;
                            if (ld != null && !string.IsNullOrEmpty(ld.Value)) wpsjob.Name = ld.Value;
                            break;
                        default:
                            tmpInputs.Add(input);
                            break;
                    }
                }
                executeInput.DataInputs = tmpInputs;
            }

            return wpsjob;
        }

        /// <summary>
        /// Builds the wps job parameters.
        /// </summary>
        /// <returns>The wps job parameters.</returns>
        /// <param name="context">Context.</param>
        /// <param name="executeInput">Execute input.</param>
        public static List<KeyValuePair<string, string>> BuildWpsJobParameters(IfyContext context, Execute executeInput) {
            context.LogDebug(context, string.Format("Building job parameters from execute request"));
            List<KeyValuePair<string, string>> output = new List<KeyValuePair<string, string>>();
            foreach (var d in executeInput.DataInputs) {
                context.LogDebug(context, string.Format("Input: " + d.Identifier.Value));
                if (d.Data != null && d.Data.Item != null) {
                    if (d.Data.Item is LiteralDataType) {
                        context.LogDebug(context, string.Format("Value is LiteralDataType"));
                        output.Add(new KeyValuePair<string, string>(d.Identifier.Value, ((LiteralDataType)(d.Data.Item)).Value));
                    } else if (d.Data.Item is ComplexDataType) {
                        context.LogDebug(context, string.Format("Value is ComplexDataType"));
                        throw new Exception("Data Input ComplexDataType not yet implemented");
                    } else if (d.Data.Item is BoundingBoxType) {
                        //for BoundingBoxType, webportal creates LowerCorner and UpperCorner
                        //we just need to save both values as a concatained string
                        context.LogDebug(context, string.Format("Value is BoundingBoxType"));
                        var bbox = d.Data.Item as BoundingBoxType;
                        var bboxVal = (bbox != null && bbox.UpperCorner != null && bbox.LowerCorner != null) ? bbox.LowerCorner.Replace(" ", ",") + "," + bbox.UpperCorner.Replace(" ", ",") : "";
                        output.Add(new KeyValuePair<string, string>(d.Identifier.Value, bboxVal));
                    } else {
                        throw new Exception("unhandled type of Data");
                    }
                } else if (d.Reference != null) {
                    context.LogDebug(context, string.Format("Value is InputReferenceType"));
                    if (!string.IsNullOrEmpty(d.Reference.href)) {
                        output.Add(new KeyValuePair<string, string>(d.Identifier.Value, d.Reference.href));
                    } else if (d.Reference.Item != null) {
                        throw new Exception("Data Input InputReferenceType item not yet implemented");
                    }
                }
            }
            return output;
        }

        /// <summary>
        /// Updates the job from execute response.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="wpsjob">Wpsjob.</param>
        /// <param name="execResponse">Exec response.</param>
        public void UpdateJobFromExecuteResponse(IfyContext context, ExecuteResponse execResponse) {
            context.LogDebug(this, string.Format("Creating job from execute response"));
            Uri uri = new Uri(execResponse.statusLocation.ToLower());

            //create WpsJob
            context.LogDebug(this, string.Format("Get identifier from status location"));
            string identifier = null;
            NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);
            if (!string.IsNullOrEmpty(nvc["id"])) {
                identifier = nvc["id"];
            } else {
                context.LogDebug(this, string.Format("identifier does not contain the key id in the query"));

                //statusLocation url is different for gpod
                if (uri.AbsoluteUri.Contains("gpod.eo.esa.int")) {
                    context.LogDebug(this, string.Format("identifier taken from gpod url : " + uri.AbsoluteUri));
                    identifier = uri.AbsoluteUri.Substring(uri.AbsoluteUri.LastIndexOf("status") + 7);
                }
                //statuslocation url is different for pywps
                else if (uri.AbsoluteUri.Contains("pywps")) {
                    identifier = uri.AbsoluteUri;
                    identifier = identifier.Substring(identifier.LastIndexOf("pywps-") + 6);
                    identifier = identifier.Substring(0, identifier.LastIndexOf(".xml"));
                }
            }
            context.LogDebug(this, string.Format("identifier = " + identifier));
            this.RemoteIdentifier = identifier;

            if (string.IsNullOrEmpty(this.StatusLocation)) this.StatusLocation = execResponse.statusLocation;

            UpdateStatusFromExecuteResponse(execResponse);

            this.Store();
        }

        /// <summary>
        /// Updates the status from execute response.
        /// Also get the Creation Time to fill End time in case of Succeeded job
        /// </summary>
        /// <param name="response">Response.</param>
        public void UpdateStatusFromExecuteResponse(ExecuteResponse response) {
            if (response.Status == null) this.Status = WpsJobStatus.NONE;
            else if (response.Status.Item is ProcessAcceptedType) this.Status = WpsJobStatus.ACCEPTED;
            else if (response.Status.Item is ProcessStartedType) this.Status = WpsJobStatus.STARTED;
            else if (response.Status.Item is ProcessSucceededType) this.Status = IsResponseFromCoordinator(response) ? WpsJobStatus.COORDINATOR : WpsJobStatus.SUCCEEDED;
            else if (response.Status.Item is ProcessFailedType) this.Status = WpsJobStatus.FAILED;
            else this.Status = WpsJobStatus.NONE;

            try {
                var endtime = response.Status.creationTime.ToUniversalTime();
                if (this.Status == WpsJobStatus.SUCCEEDED && this.EndTime == DateTime.MinValue && (this.CreatedTime.ToString() != endtime.ToString()) && this.CreatedTime < endtime) this.EndTime = endtime;
            }catch(Exception){}

            //if(this.Status == WpsJobStatus.COORDINATOR){
            //    var coordinatorsOutput = response.ProcessOutputs.First(po => po.Identifier.Value.Equals("coordinatorIds"));
            //    var coordinatorsOutput = response.ProcessOutputs.First(po => po.Identifier.Value.Equals("coordinatorIds"));
            //    var item = ((DataType)(coordinatorsOutput.Item)).Item as ComplexDataType;
            //    var data = ServiceStack.Text.JsonSerializer.DeserializeFromString<CoordinatorDataResponse>(item.Text);
            //    if (data != null && data.coordinatorsId != null && data.coordinatorsId.Count > 0){
            //        var url = data.coordinatorsId[0].store_path;
            //        if (url != null){
            //            this.StatusLocation = url;
            //        }
            //    }
            //}
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
                    using (HttpWebResponse httpWebResponse = (HttpWebResponse)executeHttpRequest.GetResponse()) {
                        using (Stream responseStream = httpWebResponse.GetResponseStream()) {
                            responseStream.CopyTo(remoteWpsResponseStream);
                        }
                        remoteWpsResponseStream.Seek(0, SeekOrigin.Begin);
                        string str = new StreamReader(remoteWpsResponseStream).ReadToEnd();
                        context.LogDebug(this, "Status response : " + str);

                    }

                } catch (WebException we) {
                    context.LogError(this, string.Format(we.Message));

                    //PATCH, waiting for http://project.terradue.com/issues/13615 to be resolved
                    if (StatusLocation.Contains("gpod.eo.esa.int") && we.Response != null) {
                        using (var remotestream = ((HttpWebResponse)we.Response).GetResponseStream()) remotestream.CopyTo(remoteWpsResponseStream);
                        remoteWpsResponseStream.Seek(0, SeekOrigin.Begin);
                        try {
                            execResponse = (OpenGis.Wps.ExecuteResponse)WpsFactory.ExecuteResponseSerializer.Deserialize(remoteWpsResponseStream);
                        }catch(Exception e){
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
                        return execResponse;
                    }
                    throw new WpsProxyException("Error proxying Status location", we);
				}catch(Exception e){
					context.LogError(this, string.Format(e.Message));
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
        /// Gets the result osd URL.
        /// </summary>
        /// <returns>The result osd URL.</returns>
        /// <param name="execResponse">Exec response.</param>
        public static string GetResultOsdUrl(ExecuteResponse execResponse) {
            // Search for an Opensearch Description Document ouput url
            var result_osd = execResponse.ProcessOutputs.Where(po => po.Identifier.Value.Equals("result_osd"));
            if (result_osd.Count() > 0) {
                var po = result_osd.First();
                //Get result Url
                if (po.Item is DataType && ((DataType)(po.Item)).Item != null) {
                    var item = ((DataType)(po.Item)).Item as ComplexDataType;
                    var reference = item.Reference as OutputReferenceType;
                    return reference.href;
                } else if (po.Item is OutputReferenceType) {
                    var reference = po.Item as OutputReferenceType;
                    return reference.href;
                }
                throw new ImpossibleSearchException("Ouput result_osd found but no Url set");
            }
            return null;
        }

        /// <summary>
        /// Gets the result metadatad URL.
        /// </summary>
        /// <returns>The result metadatad URL.</returns>
        /// <param name="execResponse">Exec response.</param>
		public static string GetResultMetadatadUrl(ExecuteResponse execResponse) {
            // Search for an Opensearch Description Document ouput url
            var result_osd = execResponse.ProcessOutputs.Where(po => po.Identifier.Value.Equals("result_metadata"));
            if (result_osd.Count() > 0) {
                var po = result_osd.First();
                string url = null;
                //Get result Url
                if (po.Item is DataType && ((DataType)(po.Item)).Item != null) {
					if (((DataType)(po.Item)).Item is ComplexDataType) {
						var item = ((DataType)(po.Item)).Item as ComplexDataType;
						var reference = item.Reference as OutputReferenceType;
						return reference.href;
					} else if (((DataType)(po.Item)).Item is LiteralDataType) { 
						var item = ((DataType)(po.Item)).Item as LiteralDataType;
						return item.Value;
					}
                } else if (po.Item is OutputReferenceType) {
                    var reference = po.Item as OutputReferenceType;
                    return reference.href;
                }
                throw new ImpossibleSearchException("Ouput result_metadata found but no Url set");
            }
            return null;
        }

        public static string GetResultHtmlUrl(ExecuteResponse execResponse) {
            // Search for an Opensearch Description Document ouput url
            var result_html = execResponse.ProcessOutputs.Where(po => po.Identifier.Value.Equals("result_html"));
            if (result_html.Count() > 0) {
                var po = result_html.First();
                //Get result Url
                if (po.Item is DataType && ((DataType)(po.Item)).Item != null) {
                    var item = ((DataType)(po.Item)).Item as ComplexDataType;
                    var reference = item.Reference as OutputReferenceType;
                    return reference.href;
                } else if (po.Item is OutputReferenceType) {
                    var reference = po.Item as OutputReferenceType;
                    return reference.href;
                }
                throw new ImpossibleSearchException("Ouput result_html found but no Url set");
            }
            return null;
        }

        public static string GetResultMetalinkUrl(ExecuteResponse execResponse) {
            // Search for an Opensearch Description Document ouput url

            foreach (OutputDataType output in execResponse.ProcessOutputs) {
                if (output.Item is DataType && ((DataType)(output.Item)).Item != null) {
                    var item = ((DataType)(output.Item)).Item as ComplexDataType;
                    if (item.Any != null && item.Any[0].LocalName != null) {
                        if (item.Any[0].LocalName.Equals("RDF")) {
                            //TODO: feed = CreateFeedForRDF(item.Any[0], request.jobid, context.BaseUrl);
                        } else if (item.Any[0].LocalName.Equals("metalink")) {
                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(item.Any[0].OuterXml);
                            XmlNamespaceManager xmlns = new XmlNamespaceManager(doc.NameTable);
                            xmlns.AddNamespace("ml", "http://www.metalinker.org");
                            var onlineResource = doc.SelectNodes("ml:metalink/ml:files/ml:file/ml:resources/ml:url", xmlns);
                            foreach (XmlNode node in onlineResource) {
                                string url = node.InnerText;
                                if (url.Contains(".atom")) return url;
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the result URL.
        /// </summary>
        /// <returns>The result URL.</returns>
        /// <param name="execResponse">Exec response.</param>
        public static string GetResultUrl(ExecuteResponse execResponse) {
            var url = GetResultOsdUrl(execResponse);
            if (string.IsNullOrEmpty(url)) url = GetResultMetadatadUrl(execResponse);
            if (string.IsNullOrEmpty(url)) url = GetResultHtmlUrl(execResponse);
            if (string.IsNullOrEmpty(url)) url = GetResultMetalinkUrl(execResponse);
            return url;
        }

        public static string GetJobOwsUrl(ExecuteResponse execResponse) {
            // Search for an Opensearch Description Document ouput url
            var result_osd = execResponse.ProcessOutputs.Where(po => po.Identifier.Value.Equals("job_ows"));
            if (result_osd.Count() > 0) {
                var po = result_osd.First();
                //Get result Url
                if (po.Item is DataType && ((DataType)(po.Item)).Item != null) {
                    var item = ((DataType)(po.Item)).Item as ComplexDataType;
                    var reference = item.Reference as OutputReferenceType;
                    return reference.href;
                } else if (po.Item is OutputReferenceType) {
                    var reference = po.Item as OutputReferenceType;
                    return reference.href;
                }
                throw new ImpossibleSearchException("Ouput job_ows found but no Url set");
            }
            return null;
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
                                                    + (content as ExceptionReport).Exception[0].ExceptionText[0]);

            if (!(content is ExecuteResponse))
                throw new ImpossibleSearchException("WPS job status did not return an ExecuteResponse : "
                                                    + content.ToString());

            ExecuteResponse execResponse = content as ExecuteResponse;

            //Go through results
            if (execResponse.ProcessOutputs == null || execResponse.ProcessOutputs.Count == 0)
                return new AtomFeedOpenSearchable(new AtomFeed());

            // Search for an Opensearch Description Document ouput url
            var url = GetResultOsdUrl(execResponse);
            if (!string.IsNullOrEmpty(url)) {
                OpenSearchUrl osUrl = null;
                try {
                    osUrl = new OpenSearchUrl(url);
                } catch (Exception) {
                    throw new ImpossibleSearchException("Ouput result_osd found invalid url : " + url);
                }

                var settings = MasterCatalogue.OpenSearchFactorySettings;
                return SandboxOpenSearchable.CreateSandboxOpenSearchable(osUrl, settings);
            }

            // Search for a static metadata file
            url = GetResultMetadatadUrl(execResponse);
            if (string.IsNullOrEmpty(url)) url = GetResultHtmlUrl(execResponse);
            if (!string.IsNullOrEmpty(url)) {
                AtomFeed feed = null;
                try {
                    HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                    httpRequest.Credentials = GetCredentials();
                    if (httpRequest.Credentials != null)
                        httpRequest.PreAuthenticate = true;
                    using (var httpResp = httpRequest.GetResponse()) {
                        using (var streamReader = new StreamReader(httpResp.GetResponseStream())) {
                            feed = AtomFeed.Load(XmlReader.Create(streamReader));
                            feed.Id = url;
                        }
                    }
                } catch (Exception e) {
                    throw new ImpossibleSearchException("Ouput result_metadata found but impossible to load url : " + url + e.Message);
                }
                return new AtomFeedOpenSearchable(feed);
            }

            url = GetResultMetalinkUrl(execResponse);
            if (!string.IsNullOrEmpty(url)) {
                AtomFeed feed = null;
                try {
                    HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                    httpRequest.Credentials = GetCredentials();
                    if (httpRequest.Credentials != null) httpRequest.PreAuthenticate = true;

                    NetworkCredential credentials = GetCredentials();
                    var uri = new UriBuilder(url);
                    HttpWebRequest atomRequest = WpsProvider.CreateWebRequest(url, credentials, context.Username);
                    atomRequest.Accept = "*/*";
                    atomRequest.UserAgent = "Terradue TEP";

                    Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter();
                    try {
                        using (var atomResponse = (HttpWebResponse)atomRequest.GetResponse()) {
                            using (var atomResponseStream = new MemoryStream()) {
                                using (var stream = atomResponse.GetResponseStream())
                                    stream.CopyTo(atomResponseStream);
                                atomResponseStream.Seek(0, SeekOrigin.Begin);
                                var sr = XmlReader.Create(atomResponseStream);
                                atomFormatter.ReadFrom(sr);
                                sr.Close();
                            }
                        }
                    } catch (Exception e) {
                        context.LogError(this, e.Message);
                        throw e;
                    }
                    feed = new AtomFeed(atomFormatter.Feed);
                    feed.Id = url;

                } catch (Exception e) {
                    throw new ImpossibleSearchException("Ouput result_metadata found but impossible to load url : " + url + e.Message);
                }
                return new AtomFeedOpenSearchable(feed);
            }

            return new ExecuteResponseOutputOpenSearchable(execResponse, context);
        }

        /// <summary>
        /// Check if the response is from a coordinator.
        /// </summary>
        /// <returns><c>true</c>, if response is from a coordinator, <c>false</c> otherwise.</returns>
        /// <param name="response">Response.</param>
        public static bool IsResponseFromCoordinator(ExecuteResponse response) {
            try {
                var coordinatorsIds = response.ProcessOutputs.Where(po => po.Identifier.Value.Equals("coordinatorIds"));
                if (coordinatorsIds.Count() > 0) return true;
            } catch (Exception) { }
            return false;
        }

        /// <summary>
        /// Gets the job atom feed.
        /// </summary>
        /// <returns>The job atom feed.</returns>
        /// <param name="feed">Feed.</param>
        public OwsContextAtomFeed GetJobAtomFeed(){

            OwsContextAtomFeed feed = new OwsContextAtomFeed();
            OwsContextAtomEntry entry = new OwsContextAtomEntry();

            entry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            entry.Summary = new TextSyndicationContent(this.Name);
            entry.Title = new TextSyndicationContent(this.Name);

            if (this.Owner != null) {
                entry.Authors.Add(new SyndicationPerson {
                    Name = this.Owner.Caption,
                    Email = this.Owner.Email,
                    Uri = context.GetConfigValue("BaseUrl") + "/#!user/details/" + this.Owner.Identifier
                });
            }

            feed.Items = new List<OwsContextAtomEntry> { entry };
            return feed;
        }

        /// <summary>
        /// Gets the job atom feed from ows URL.
        /// </summary>
        /// <returns>The job atom feed from ows URL.</returns>
        public OwsContextAtomFeed GetJobAtomFeedFromOwsUrl(string appId){
            var feed = new OwsContextAtomFeed();
            OwsContextAtomEntry entry = null;

            if (string.IsNullOrEmpty(OwsUrl)) return null;

            var urlb = new UriBuilder(OwsUrl);
            var user = UserTep.FromId(context, context.UserId);
            NameValueCollection query = HttpUtility.ParseQueryString(urlb.Query);
            query.Set("apikey", user.GetSessionApiKey());
            string[] queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urlb.Query = string.Join("&", queryString);
            try {
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(urlb.Uri.AbsoluteUri);
                using (var resp = httpRequest.GetResponse()) {
                    using (var stream = resp.GetResponseStream()) {
                        feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(stream);
                        entry = feed.Items.First();
                    }
                }
            } catch (Exception e) {
                context.LogError(this, e.Message);
                return null;
            }
            if (entry == null) return null;

            //update title, as it may have change
            entry.Title = new TextSyndicationContent(this.Name);
            entry.Summary = new TextSyndicationContent(BuildOwsSummaryFromFeed(entry, appId));


            feed.Items = new List<OwsContextAtomEntry> { entry };
            return feed;
        }

        public OwsContextAtomFeed UpdateResultLinksForPublish(OwsContextAtomFeed feed, string index){
            var selfResultUri = new Uri(string.Format("{0}/{1}/search?cat={2}", context.GetConfigValue("catalog-baseurl"), index, this.Identifier.Replace("-", "")));
            var entry = feed.Items.FirstOrDefault<OwsContextAtomEntry>();
            var matchLinks = entry.Links.Where(l => l.RelationshipType == "results").ToArray();
            string self = null;
            foreach (var link in matchLinks) {
                if (link.Title == "Job results") {
                    self = link.Uri.AbsoluteUri;
                    entry.Links.Remove(link);
                }
            }
            if (self != null) {
                var link = new SyndicationLink(selfResultUri, "results", "Job results", "application/atom+xml", 0);
                link.AttributeExtensions.Add(new System.Xml.XmlQualifiedName("level"), "info");
                entry.Links.Add(link);
            }

            entry.Links.Add(new SyndicationLink(selfResultUri, "related", "Job results (correlated)", "application/atom+xml", 0));

            return new OwsContextAtomFeed { Items = new List<OwsContextAtomEntry> { entry } };
        }

        public OwsContextAtomFeed GetJobResultsAtomFeedFromLink(SyndicationLink link) {
            var feed = new OwsContextAtomFeed();

            var settings = MasterCatalogue.OpenSearchFactorySettings;
            var os = OpenSearchFactory.FindOpenSearchable(settings, link.Uri);
            var osr = MasterCatalogue.OpenSearchEngine.Query(os, new NameValueCollection());

            Stream stream = new MemoryStream();
            osr.SerializeToStream(stream);
            stream.Seek(0, SeekOrigin.Begin);
            feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(stream);
            return feed;
        }

        private string BuildOwsSummaryFromFeed(OwsContextAtomEntry entry, string appId = null){
            try {
                var title = this.Name;
                var author = entry.Authors != null && entry.Authors[0] != null ? entry.Authors[0].Name : "";
                var creators = entry.ElementExtensions.ReadElementExtensions<string>("creator", "http://purl.org/dc/elements/1.1/");
                var creator = creators.Count > 0 ? creators[0] : "";
                var startDate = entry.Date.StartDate.ToUniversalTime().ToString("o");
                var endDate = entry.Date.EndDate.ToUniversalTime().ToString("o");

                var shareUri = GetJobShareUri(appId);

                var warningText = "restricted to users with access to service used to generate the job";

                var html = string.Format("<table>" +
                                         "<tr><td>title</td><td>{0}</td></tr>" +
                                         "<tr><td>author</td><td>{1}</td></tr>" +
                                         "<tr><td>generator</td><td>{2}</td></tr>" +
                                         "<tr><td>submission</td><td>{3}</td></tr>" +
                                         "<tr><td>completion</td><td>{4}</td></tr>" +
                                         "</table><a target='_blank' href='{5}'><i class='fa fa-arrow-right'></i> Go to job</a> <small><a href='javascript://' title='{6}'><i class='fa fa-warning'></i></a></small>", title, author, creator, startDate, endDate, shareUri.AbsoluteUri, warningText);


                return html;
            }catch(Exception e){
                context.LogError(this, e.Message);
                return this.Name;
            }
        }

        /// <summary>
        /// Gets the job self URI.
        /// </summary>
        /// <returns>The job self URI.</returns>
        public Uri GetJobSelfUri(){
            var entityType = EntityType.GetEntityType(typeof(WpsJob));
            var selfUrlB = new UriBuilder(string.Format("{0}/{1}/search?id={2}&key={3}", context.BaseUrl, entityType.Keyword, this.Identifier, this.AccessKey));
            return selfUrlB.Uri;
        }

        /// <summary>
        /// Gets the job share URI.
        /// </summary>
        /// <returns>The job share URI.</returns>
        /// <param name="appId">App identifier.</param>
        public Uri GetJobShareUri(string appId = null){
            var selfUri = GetJobSelfUri();
            var shareUrlB = new UriBuilder(string.Format("{0}/share?url={1}{2}", context.BaseUrl, selfUri.AbsoluteUri, !string.IsNullOrEmpty(appId) ? "&id=" + appId : ""));
            return shareUrlB.Uri;
        }


        #region IEntitySearchable implementation
        public override object GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
                case "correlatedTo":
                    var settings = MasterCatalogue.OpenSearchFactorySettings;
                    var urlBOS = new UrlBasedOpenSearchable(context, new OpenSearchUrl(value), settings);
                    var entity = urlBOS.Entity;
                    if (entity is EntityList<ThematicCommunity>) {
                        var entitylist = entity as EntityList<ThematicCommunity>;
                        var items = entitylist.GetItemsAsList();
                        if (items.Count > 0) {
                            return new KeyValuePair<string, string>("DomainId", items[0].Id.ToString());
                        }
                    } else if (entity is EntityList<WpsProcessOffering>) {
                        var entitylist = entity as EntityList<WpsProcessOffering>;
                        var items = entitylist.GetItemsAsList();
                        if (items.Count > 0) {
                            var processIds = "";
                            foreach (var item in items) processIds += item.Identifier + ",";
                            processIds = processIds.Trim(",".ToCharArray());
                            return new KeyValuePair<string, string>("ProcessId", processIds);
                        }
                    } else if (entity is MultiGenericOpenSearchable) {
                        if (urlBOS.Items != null) {
                            var processIds = "";
                            foreach (var item in urlBOS.Items) processIds += item.Identifier + ",";
                            processIds = processIds.Trim(",".ToCharArray());
                            if (string.IsNullOrEmpty(processIds)) processIds = "0";
                            return new KeyValuePair<string, string>("ProcessId", processIds);
                        }
                    }
                    return new KeyValuePair<string, string>();
                case "appId":
                    return new KeyValuePair<string, string>("AppIdentifier", value);
                default:
                    return base.GetFilterForParameter(parameter, value);
            }
        }

        #endregion



        #region IAtomizable implementation

        public override NameValueCollection GetOpenSearchParameters() {
            NameValueCollection nvc = base.GetOpenSearchParameters();
            nvc.Add("basic", "{t2:basic?}");
            nvc.Add("appId", "{t2:appId?}");
            return nvc;
        }

        public override bool IsPostFiltered(NameValueCollection parameters) {
            foreach (var key in parameters.AllKeys) {
                switch (key) {
                    default:
                        break;
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
            Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier + "&key=" + this.AccessKey);

            AtomItem result = new AtomItem();
            string statusloc = this.StatusLocation;

            if (string.IsNullOrEmpty(parameters["basic"]) || parameters["basic"] != "true") {
                result = GetFullWpsJobAtomItem();
                if (result == null) {
                    result = new AtomItem();
                    ispublic = false;
                }
            }

            try {
                if (provider == null) provider = (WpsProvider)WpsProvider.FromIdentifier(context, this.WpsId);
                result.Categories.Add(new SyndicationCategory("provider", null, provider.Name));
                if (provider.Proxy) statusloc = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + this.Identifier;

            } catch (Exception e) {
                //if provider not on db, then it is proxied
                statusloc = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + this.Identifier;
            }

            try {
                if (process == null) process = (WpsProcessOffering)WpsProcessOffering.FromIdentifier(context, this.ProcessId);
                result.Categories.Add(new SyndicationCategory("process", null, process.Name));
            } catch (Exception e) {
            }

            if (this.NbResults != -1){
                result.Categories.Add(new SyndicationCategory("nbresults", null, "" + this.NbResults));
            }

            result.Id = id.ToString();
            result.Title = new TextSyndicationContent(identifier);
            result.Content = new TextSyndicationContent(name);

            result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            result.Summary = new TextSyndicationContent(name);
            result.ReferenceData = this;

            result.PublishDate = new DateTimeOffset(this.CreatedTime);
            if(this.EndTime != DateTime.MinValue) result.LastUpdatedTime = new DateTimeOffset(this.EndTime);

            var basepath = new UriBuilder(context.BaseUrl);
            basepath.Path = "user";
            string usrUri = basepath.Uri.AbsoluteUri + "/" + Owner.Username;
            string usrName = (!String.IsNullOrEmpty(Owner.FirstName) && !String.IsNullOrEmpty(Owner.LastName) ? Owner.FirstName + " " + Owner.LastName : Owner.Username);
            SyndicationPerson author = new SyndicationPerson(null, usrName, usrUri);
            author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", Owner.Username));
            result.Authors.Add(author);
            result.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));
            Uri share = GetJobShareUri();
            result.Links.Add(new SyndicationLink(share, "via", name, "application/atom+xml", 0));
            result.Links.Add(new SyndicationLink(new Uri(statusloc), "alternate", "statusLocation", "application/atom+xml", 0));
            if (!string.IsNullOrEmpty(OwsUrl))result.Links.Add(new SyndicationLink(new Uri(OwsUrl), "alternate", "owsUrl", "application/atom+xml", 0));
            result.Links.Add(new SyndicationLink(new Uri(this.StatusLocation), "alternate", "statusLocationDirect", "application/atom+xml", 0));
            Uri sharedUrlUsr = null, sharedUrlCommunity = null;

            //if shared with users
            if (IsSharedToUser()) {
                sharedUrlUsr = new Uri(string.Format("{0}/user/search?correlatedTo={1}", context.BaseUrl, HttpUtility.UrlEncode(id.AbsoluteUri)));
                status = "restricted";
            }
            if (IsSharedToCommunity()) {
                sharedUrlCommunity = new Uri(string.Format("{0}/community/search?correlatedTo={1}", context.BaseUrl, HttpUtility.UrlEncode(id.AbsoluteUri)));
                status = "restricted";
            }

            //for owner only, we give the link to know with who the wpsjob is shared
            if (Owner.Id == context.UserId) {
                if (sharedUrlUsr != null) result.Links.Add(new SyndicationLink(sharedUrlUsr, "results", name, "application/atom+xml", 0));
                if (sharedUrlCommunity != null) result.Links.Add(new SyndicationLink(sharedUrlCommunity, "results", name, "application/atom+xml", 0));
            }

            try {
                result.ElementExtensions.Add("Parameters", "http://standards.terradue.com", WpsJobParameter.GetList(this.Parameters));
            } catch (Exception) {
                result.ElementExtensions.Add("Parameters", "http://standards.terradue.com", new List<WpsJobParameter>());
            }

            result.Categories.Add(new SyndicationCategory("remote_identifier", null, this.RemoteIdentifier));
            result.Categories.Add(new SyndicationCategory("visibility", null, status));
            result.Categories.Add(new SyndicationCategory("status", null, this.Status.ToString()));
            if (!string.IsNullOrEmpty(this.WpsVersion)) result.Categories.Add(new SyndicationCategory("service_version", null, "" + this.WpsVersion));
            return result;
        }

        public string ExtractProviderContact(string contact) {
            if (!string.IsNullOrEmpty(contact)) {
                if (contact.Contains("@")) {
                    //in case contact contains more than an email address
                    var contacts = contact.Split(" ".ToArray());
                    foreach (var c in contacts) {
                        if (c.Contains("@")) {
                            return c;
                        }
                    }
                }
            }
            return null;
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
                string[] identifierParams = this.ProcessId.Split("-".ToCharArray());
                if (identifierParams.Length == 3) {
                    switch (identifierParams[0]) {
                        case "one":
                            CloudWpsFactory wpstep = new CloudWpsFactory(context);
                            if (this.IsPublic()) wpstep.StartDelegate(this.OwnerId);
                            try {
                                context.LogDebug(this, "Get process -- " + identifierParams[1] + " -- " + identifierParams[2]);
                                process = wpstep.CreateWpsProcessOfferingForOne(identifierParams[1], identifierParams[2]);
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
                                      "&version=" + provider.WPSVersion +
                                          "&identifier=" + identifier);
            Uri executeUri = new Uri(providerUrl + "?service=WPS" +
                                         "&request=Execute" +
                                     "&version=" + provider.WPSVersion +
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
            operation.Request.Any = (XmlElement)nodes[0];
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
            operation.Request.Any = (XmlElement)nodes[0];
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
            operation.Request.Any = (XmlElement)nodes[0];
            operations.Add(operation);

            offering.Operations = operations.ToArray();

            OwsContextAtomEntry entry = new OwsContextAtomEntry();

            entry.Publisher = Owner.Username;

            entry.Offerings = new List<OwcOffering> { offering };
            entry.Categories.Add(new SyndicationCategory("WpsOffering"));
            if (process.Commercial) {
                var contact = ExtractProviderContact(provider.Contact);

                if (!string.IsNullOrEmpty(contact)) {
                    entry.Categories.Add(new SyndicationCategory("contact", null, contact));
                }
            }

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

        public string StringStatus {
            get {
                switch (Status) {
                    case WpsJobStatus.NONE:
                        return "NONE";
                    case WpsJobStatus.ACCEPTED:
                        return "ACCEPTED";
                    case WpsJobStatus.STARTED:
                        return "STARTED";
                    case WpsJobStatus.PAUSED:
                        return "PAUSED";
                    case WpsJobStatus.SUCCEEDED:
                        return "SUCCEEDED";
                    case WpsJobStatus.STAGED:
                        return "STAGED";
                    case WpsJobStatus.FAILED:
                        return "FAILED";
                    case WpsJobStatus.COORDINATOR:
                        return "COORDINATOR";
                    default:
                        return "";
                }
            }
        }

        /// <summary>
        /// Gets the result count.
        /// </summary>
        /// <returns>The result count.</returns>
        public void UpdateResultCount() {

            long nbresults = 0;

            //check status first
            switch (Status) {
                case WpsJobStatus.NONE:
                case WpsJobStatus.ACCEPTED:
                case WpsJobStatus.STARTED:
                case WpsJobStatus.PAUSED:
                    return;
                case WpsJobStatus.FAILED:
                    nbresults = 0;
                    break;
                case WpsJobStatus.SUCCEEDED:
                    nbresults = GetResultOsdResultCount();
                    break;
                case WpsJobStatus.STAGED:
                    nbresults = GetOpenSearchableResultCount(StatusLocation);
                    break;
                case WpsJobStatus.COORDINATOR:
                    nbresults = GetOpenSearchableResultCount(StatusLocation);
                    break;
                default:
                    break;
            }

            this.NbResults = (int)nbresults;
            this.Store();
        }

        /// <summary>
        /// Gets the recast result count.
        /// </summary>
        /// <returns>The recast result count.</returns>
        private long GetOpenSearchableResultCount(string url) {
            try {
                var nvc = new NameValueCollection();
                nvc.Set("count", "0");
                var ios = OpenSearchFactory.FindOpenSearchable(MasterCatalogue.OpenSearchFactorySettings, new OpenSearchUrl(url));
                var result = MasterCatalogue.OpenSearchEngine.Query(ios, nvc);
                return result.TotalResults;
            } catch (Exception e) {
                if (new Uri(url).Host == new Uri(ProductionResultHelper.catalogBaseUrl).Host) {
                    return 0;
                }
                throw e; 
            }
        }

        private long GetOpenSearchableMetadataResultCount(string url) {
            try {
                var nvc = new NameValueCollection();
                nvc.Set("count", "0");
                WpsJobProductOpenSearchable wpsjobProductOs = new WpsJobProductOpenSearchable(this, context);
                var result = MasterCatalogue.OpenSearchEngine.Query(wpsjobProductOs, nvc);
                return result.TotalResults;
            } catch (Exception e) { throw e; }//return 0; }
        }

        /// <summary>
        /// Gets the result osd result count.
        /// </summary>
        /// <returns>The result osd result count.</returns>
        private long GetResultOsdResultCount() {
            try {
                var jobresponse = GetStatusLocationContent();
                if (!(jobresponse is ExecuteResponse)) return 0;
                var execResponse = jobresponse as ExecuteResponse;

                var url = GetResultOsdUrl(execResponse);
                if (!string.IsNullOrEmpty(url)) {
                    return GetOpenSearchableResultCount(url);
                } else {
                    url = GetResultMetadatadUrl(execResponse);
                    if (!string.IsNullOrEmpty(url)) {
                        return GetOpenSearchableMetadataResultCount(url);
                    } else{
                        url = GetResultHtmlUrl(execResponse);
                        if (!string.IsNullOrEmpty(url)) { 
                            return GetOpenSearchableMetadataResultCount(url);
                        } else {
                            throw new Exception("Unable to get wpsjob result url");
                        }
                    }
                }
            } catch (Exception e) { throw e; }//return 0; }
        }
    }

	/**********************************************************************************************************************/
	/**********************************************************************************************************************/
	/**********************************************************************************************************************/

	/// <summary>
	/// Wps job status.
	/// </summary>
	public enum WpsJobStatus {
        NONE = 0, //default status
        ACCEPTED = 1, //wps job has been accepted
        STARTED = 2, //wps job has been started
        PAUSED = 3, //wps job has been paused
        SUCCEEDED = 4, //wps job is succeeded
        STAGED = 5, //wps job has been staged on store
        FAILED = 6, //wps job has failed
        COORDINATOR = 7 //wps job is a coordinator
    }

    [DataContract]
    public class WpsJobParameter {

        public static List<WpsJobParameter> GetList(List<KeyValuePair<string,string>> parameters){
            var result = new List<WpsJobParameter>();
            if(parameters != null){
                foreach(var p in parameters){
                    result.Add(new WpsJobParameter{ Name = p.Key, Value = p.Value });
                }
            }
            return result;
        }

        [DataMember]
        string Name { get; set; }

        [DataMember]
        string Value { get; set; }
    }

    [DataContract]
    public class CoordinatorsId {
        [DataMember]
        public string oozieId { get; set; }
        [DataMember]
        public string wpsId { get; set; }
        [DataMember]
        public string store_path { get; set; }
    }

    [DataContract]
    public class CoordinatorDataResponse {
        [DataMember]
        public List<CoordinatorsId> coordinatorsId { get; set; }
    }
}


