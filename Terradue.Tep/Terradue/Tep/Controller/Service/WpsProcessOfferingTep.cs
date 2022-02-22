using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using IO.Swagger.Model;
using OpenGis.Wps;
using Terradue.OpenSearch;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;

namespace Terradue.Tep {
    [EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above, AllowsKeywordSearch = true)]
    public class WpsProcessOfferingTep : WpsProcessOffering {

        public WpsProcessOfferingTep(IfyContext context) : base(context) {
        }

        public static new WpsProcessOfferingTep FromIdentifier(IfyContext context, string identifier) {
            var p = new WpsProcessOfferingTep(context);
            p.Identifier = identifier;
            p.Load();
            return p;
        }

        public static WpsProcessOfferingTep Copy(WpsProcessOfferingTep service, IfyContext context) {
            WpsProcessOfferingTep newservice = new WpsProcessOfferingTep(context);
            newservice.OwnerId = context.UserId;
            newservice.UserId = context.UserId;
            newservice.Identifier = Guid.NewGuid().ToString();
            newservice.Name = service.Name;
            newservice.Description = service.Description;
            newservice.Url = service.Url;
            newservice.Version = service.Version;
            newservice.IconUrl = service.IconUrl;
            newservice.ValidationUrl = service.ValidationUrl;
            newservice.TermsConditionsUrl = service.TermsConditionsUrl;
            newservice.TermsConditionsText = service.TermsConditionsText;
            newservice.Domain = service.Domain;
            newservice.Tags = service.Tags;            
            newservice.RemoteIdentifier = service.RemoteIdentifier;
            newservice.Available = service.Available;    
            newservice.Commercial = service.Commercial;
            newservice.Provider = service.Provider;
            newservice.Geometry = service.Geometry;
            return newservice;
        }

        public string ValidateResult(string json) {
            if (string.IsNullOrEmpty(this.ValidationUrl)) return null;
            try {
                var httpRequest = (HttpWebRequest)WebRequest.Create(this.ValidationUrl);
                httpRequest.Method = "POST";
                httpRequest.Accept = "application/json";
                httpRequest.ContentType = "application/json";

                using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream())) {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    using (var httpResponse = (HttpWebResponse)httpRequest.GetResponse()) {
                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                            var result = streamReader.ReadToEnd();
                            context.LogDebug(this, "WPS service validation result : " + result);
                            return result;
                        }
                    }
                }                
            }catch(System.Exception e) {
                context.LogError(this, e.Message, e);
                throw e;
            }
        }

        /***********/
        /* WPS 3.0 */
        /***********/

        /// <summary>
        /// Is this Service WPS 3.0
        /// </summary>
        /// <returns></returns>
        public bool IsWPS3() {
            return IsWPS3(this.Url);
        }

        public static bool IsWPS3(string url) {
            if (url == null) return false;
            return url.Contains("/wps3/");
        }

        /// <summary>
        /// Get list of Processing Services in DB for domain and tags
        /// </summary>
        /// <param name="context"></param>
        /// <param name="domain"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static List<WpsProcessOffering> GetWpsProcessingOfferingsForApp(IfyContext context, Domain domain, string[] tags) {
            //get services already in DB
            EntityList<WpsProcessOffering> dbProcesses = new EntityList<WpsProcessOffering>(context);
            dbProcesses.SetFilter("DomainId", domain.Id);
            if (tags != null && tags.Count() > 0) {
                IEnumerable<IEnumerable<string>> permutations = GetPermutations(tags, tags.Count());
                var r1 = permutations.Select(subset => string.Join("*", subset.Select(t => t).ToArray())).ToArray();
                var tagsresult = string.Join(",", r1.Select(t => "*" + t + "*"));
                dbProcesses.SetFilter("Tags", tagsresult);
            }
            dbProcesses.Load();
            return dbProcesses.GetItemsAsList();
        }

        /// <summary>
        /// Get list of Processing Services from Atom Feed
        /// Each Item of the feed contains a Describe Process url
        /// </summary>
        /// <param name="context"></param>
        /// <param name="feed"></param>
        /// <param name="createProviderIfNotFound"></param>
        /// <returns></returns>
        public static List<WpsProcessOffering> GetRemoteWpsProcessingOfferingsFromUrl(IfyContext context, string url, bool createProviderIfNotFound) {
            var remoteProcesses = new List<WpsProcessOffering>();

            var items = GetRemoteWpsServiceEntriesFromUrl(context, url);

            foreach (OwsContextAtomEntry item in items) {

                var wps = GetWpsProcessOfferingFromProcessDescriptionAtomFeed(context, item);
                if (wps == null) continue;

                var describeProcessUrl = wps.Url;
                var providerBaseUrl = describeProcessUrl.Substring(0, describeProcessUrl.LastIndexOf("/"));
                var processIdentifier = describeProcessUrl.Substring(describeProcessUrl.LastIndexOf("/") + 1);
                WpsProvider wpsprovider = null;
                try {
                    wpsprovider = WpsProvider.FromBaseUrl(context, providerBaseUrl);
                } catch (System.Exception) {
                    if (createProviderIfNotFound) {
                        var urip = new Uri(providerBaseUrl);
                        wpsprovider = new WpsProvider(context);
                        wpsprovider.Identifier = urip.AbsolutePath.Contains("/wps3/processes") ?
                                                    urip.Host + urip.AbsolutePath.Substring(0, urip.AbsolutePath.IndexOf("/wps3/processes")).Replace("/", ".") :
                                                    Guid.NewGuid().ToString();
                        wpsprovider.Name = urip.AbsolutePath.Contains("/wps3/processes") ?
                                                    urip.Host + urip.AbsolutePath.Substring(0, urip.AbsolutePath.IndexOf("/wps3/processes")).Replace("/",".") :
                                                    urip.Host + urip.AbsolutePath.Replace("/", ".");
                        wpsprovider.BaseUrl = providerBaseUrl;
                        wpsprovider.StageResults = true;
                        wpsprovider.Proxy = true;
                        wpsprovider.Store();

                        wpsprovider.GrantPermissionsToAll();
                    }
                }
                if (wpsprovider != null) wps.Provider = wpsprovider;

                //case WPS 3.0
                if (IsWPS3(describeProcessUrl)) {
                    try
                    {
                        WpsProcessOffering process = GetProcessingFromDescribeProcessWps3(context, describeProcessUrl);
                        wps.RemoteIdentifier = process.RemoteIdentifier;
                        if (string.IsNullOrEmpty(wps.Name)) wps.Name = process.Name;
                        if (string.IsNullOrEmpty(wps.Description)) wps.Description = process.Description;
                        if (string.IsNullOrEmpty(wps.Version)) wps.Version = process.Version;
                    }catch(System.Exception e){
                        context.LogError(context, "Error with url '" + describeProcessUrl + "' : " + e.Message);
                        wps = null;
                    }
                }
                if (wps == null) continue;
                remoteProcesses.Add(wps);
            }
            return remoteProcesses;
        }

        public static WpsProcessOffering GetWpsProcessOfferingFromProcessDescriptionAtomFeed(IfyContext context, OwsContextAtomEntry entry) {

            var wpsOffering = entry.Offerings.FirstOrDefault(of => of.Code == "http://www.opengis.net/spec/owc/1.0/req/atom/wps");
            if (wpsOffering == null) return null;

            var wps = new WpsProcessOffering(context);

            wps.Identifier = Guid.NewGuid().ToString();
            var identifiers = entry.ElementExtensions.ReadElementExtensions<string>("identifier", "http://purl.org/dc/elements/1.1/");
            if (identifiers.Count > 0) wps.RemoteIdentifier = identifiers[0];
            wps.Name = entry.Title != null ? entry.Title.Text : "";
            wps.Description = entry.Summary != null ? entry.Summary.Text : "";

            var appIconLink = entry.Links.FirstOrDefault(l => l.RelationshipType == "icon");
            if (appIconLink != null) wps.IconUrl = appIconLink.Uri.AbsoluteUri;

            var operation = wpsOffering.Operations.FirstOrDefault(o => o.Code == "ProcessDescription");
            var href = operation.Href;
            var dpUri = new Uri(href);
            var describeProcessUrl = dpUri.GetLeftPart(UriPartial.Path);
            wps.Url = describeProcessUrl;

            operation = wpsOffering.Operations.FirstOrDefault(o => o.Code == "ValidateProcess");
            if (operation != null) wps.ValidationUrl = operation.Href;

            return wps;
        }

        public static List<OwsContextAtomEntry> GetRemoteWpsServiceEntriesFromUrl(IfyContext context, string href) {
            var result = new List<OwsContextAtomEntry>();
            var httpRequest = (HttpWebRequest)WebRequest.Create(href);

            if (context.UserId != 0)
            {
                var user = UserTep.FromId(context, context.UserId);
                var apikey = user.GetSessionApiKey();
                if (!string.IsNullOrEmpty(user.Username) && !string.IsNullOrEmpty(apikey))
                {
                    httpRequest.Headers.Set(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(user.Username + ":" + apikey)));
                }
            }

            //TODO: manage case of /description
            //TODO: manage case of total result > 20                        
            using (var resp = httpRequest.GetResponse()) {
                using (var stream = resp.GetResponseStream()) {
                    var feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(stream);
                    if (feed.Items != null) {
                        foreach (OwsContextAtomEntry item in feed.Items) {
                            result.Add(item);                            
                        }
                    }
                }
            }
            return result;
        }

        public static List<WpsProcessOffering> GetRemoteWpsServiceUrlsFromUrl(IfyContext context, string href) {
            var result = new List<WpsProcessOffering>();
            var items = GetRemoteWpsServiceEntriesFromUrl(context, href);
            foreach(var item in items) {
                var wps = GetWpsProcessOfferingFromProcessDescriptionAtomFeed(context, item);
                if (wps == null) continue;
                result.Add(wps);
            }
            return result;
        }

        /******************************/
        /* WPS 3.0 - DESCRIBE PROCESS */
        /******************************/

        /// <summary>
        /// Describe Process
        /// </summary>GetWpsProcessingFromDescribeProcess
        /// <returns></returns>
        public new object DescribeProcess() {

            if (this.IsWPS3()) {
                var wps3 = GetWps3ProcessingFromDescribeProcess(this.Url);
                return GetDescribeProcessFromWps3(wps3.Process);
            } else {
                return base.DescribeProcess();
            }
        }

        /// <summary>
        /// Get Wps Processing Offering from DescribeProcess url
        /// </summary>
        /// <param name="describeProcessUrl"></param>
        /// <returns></returns>
        public static WpsProcessOffering GetProcessingFromDescribeProcessWps3(IfyContext context, string describeProcessUrl) {
            var wps3 = GetWps3ProcessingFromDescribeProcess(describeProcessUrl);

            WpsProcessOffering process = new WpsProcessOffering(context);
            process.Identifier = Guid.NewGuid().ToString();
            process.RemoteIdentifier = wps3.Process.Id;
            process.Name = wps3.Process.Title;
            process.Description = wps3.Process.Abstract ?? wps3.Process.Title;
            process.Version = wps3.Process.Version;
            process.Url = describeProcessUrl;

            return process;
        }

        /// <summary>
        /// Get WPS3 processing From Describe Process url
        /// </summary>
        /// <param name="describeProcessUrl"></param>
        /// <returns></returns>
        public static Wps3 GetWps3ProcessingFromDescribeProcess(string describeProcessUrl) {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(describeProcessUrl);
            webRequest.Method = "GET";
            webRequest.Accept = "application/json";

            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    var result = streamReader.ReadToEnd();
                    var response = ServiceStack.Text.JsonSerializer.DeserializeFromString<Wps3>(result);
                    return response;
                }
            }
        }

        /// <summary>
        /// Get WPS3 processings from Get Capabilities url
        /// </summary>
        /// <param name="getCapabilitiesUrl"></param>
        /// <returns></returns>
        public static List<Process> GetWps3ProcessingsFromGetCapabilities(string getCapabilitiesUrl) {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(getCapabilitiesUrl);
            webRequest.Method = "GET";
            webRequest.Accept = "application/json";

            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    var result = streamReader.ReadToEnd();
                    var response = ServiceStack.Text.JsonSerializer.DeserializeFromString<List<Process>>(result);
                    return response;
                }
            }
        }

        /// <summary>
        /// Get Describe process from wps3
        /// </summary>
        /// <param name="wps3"></param>
        /// <returns></returns>
        private ProcessDescriptions GetDescribeProcessFromWps3(Process wps3) {
            ProcessDescriptions processDescriptions = new ProcessDescriptions();

            var description = new ProcessDescriptionType();
            description.Identifier = new CodeType { Value = this.Identifier };
            description.Title = new LanguageStringType { Value = this.Name };
            description.Abstract = new LanguageStringType { Value = this.Description };
            description.DataInputs = new List<InputDescriptionType>();
            description.ProcessOutputs = new List<OutputDescriptionType>();

            //inputs
            foreach (var inputWps3 in wps3.Inputs) {
                var input = new InputDescriptionType();
                input.Identifier = new CodeType { Value = inputWps3.Id };
                input.Title = new LanguageStringType { Value = inputWps3.Title };
                input.Abstract = new LanguageStringType { Value = inputWps3.Abstract };
                input.minOccurs = inputWps3.MinOccurs.ToString();
                input.maxOccurs = inputWps3.MaxOccurs.ToString();

                if (inputWps3.Input.LiteralDataDomains != null) {
                    input.LiteralData = new LiteralInputType();
                    var literaldomain = inputWps3.Input.LiteralDataDomains[0];
                    if (literaldomain.DataType != null) input.LiteralData.DataType = new DomainMetadataType { Value = literaldomain.DataType.Name, reference = literaldomain.DataType.Reference };
                    if (literaldomain.Uom != null) input.LiteralData.UOMs = new SupportedUOMsType { Default = new SupportedUOMsTypeDefault { UOM = new DomainMetadataType { Value = literaldomain.Uom.Name, reference = literaldomain.Uom.Reference } } };
                    if (literaldomain.DefaultValue != null) input.LiteralData.DefaultValue = literaldomain.DefaultValue;
                    //if (literaldomain.ValueDefinition != null) input.LiteralData.AnyValue = new ServiceModel.Ogc.Ows11.AnyValue { AnyValue =  }
                }
                description.DataInputs.Add(input);
            }

            //outputs
            var output = new OutputDescriptionType {
                Identifier = new CodeType { Value = "result_osd" },
                Title = new LanguageStringType { Value = "OpenSearch Description to the Results" },
                Abstract = new LanguageStringType { Value = "OpenSearch Description to the Results" },
                Item = new SupportedComplexDataType {
                    Default = new ComplexDataCombinationType { Format = new ComplexDataDescriptionType { MimeType = "application/opensearchdescription+xml" } },
                    Supported = new List<ComplexDataDescriptionType> { new ComplexDataDescriptionType { MimeType = "application/opensearchdescription+xml" } }
                }
            };
            description.ProcessOutputs.Add(output);

            processDescriptions.ProcessDescription = new List<ProcessDescriptionType> { description };
            return processDescriptions;
        }

        /**********************/
        /* WPS 3.0 GET STATUS */
        /**********************/

        public ProcessBriefType ProcessBrief {
            get {
                ProcessBriefType processbrief = new ProcessBriefType();
                processbrief.Identifier = new CodeType { Value = this.RemoteIdentifier };
                processbrief.Abstract = new LanguageStringType { Value = this.Description };
                processbrief.Title = new LanguageStringType { Value = this.Name };
                processbrief.processVersion = this.Version;
                return processbrief;
            }
        }
        
        /*******************/
        /* WPS 3.0 EXECUTE */
        /*******************/

        public new object Execute(OpenGis.Wps.Execute executeInput, string jobreference = null) {

            if (this.IsWPS3()) {
                context.LogDebug(this, "WPS 3.0.0 Execute");
                var creationTime = DateTime.UtcNow;
                var location = SubmitExecute(executeInput);

                ExecuteResponse response = new ExecuteResponse();
                response.statusLocation = location;

                var uri = new Uri(location);
                response.serviceInstance = string.Format("{0}://{1}/", uri.Scheme, uri.Host);
                response.Process = ProcessBrief;
                response.service = "WPS";
                response.version = "3.0.0";

                response.Status = new StatusType { Item = new ProcessAcceptedType() { Value = string.Format("Preparing job") }, creationTime = creationTime };//TODO
                return response;

                //TODO: handle case of errors
            } else {
                context.LogDebug(this, "WPS 1.0.0 Execute");
                return base.Execute(executeInput, jobreference);
            }
        }

        public string SubmitExecute(OpenGis.Wps.Execute executeInput) {

            IO.Swagger.Model.Execute execute = BuildExecute(executeInput);

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(this.Url + "/jobs");
            webRequest.Method = "POST";
            webRequest.Accept = "application/json";
            webRequest.ContentType = "application/json";

            var json = ServiceStack.Text.JsonSerializer.SerializeToString<IO.Swagger.Model.Execute>(execute);
            context.LogDebug(this, json);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
            webRequest.ContentLength = data.Length;

            using (var requestStream = webRequest.GetRequestStream()) {
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        var result = streamReader.ReadToEnd();
                        var response = ServiceStack.Text.JsonSerializer.DeserializeFromString<Wps3>(result);
                        var location = new Uri(httpResponse.Headers["Location"], UriKind.RelativeOrAbsolute);
                        if (!location.AbsoluteUri.StartsWith("http"))
                            location = new Uri(new Uri(this.Url), location);
                        return location.AbsoluteUri;
                    }
                }
            }
        }

        protected IO.Swagger.Model.Execute BuildExecute(OpenGis.Wps.Execute executeInput) {

            context.LogDebug(this, "BuildExecute");
            var wps3 = GetWps3ProcessingFromDescribeProcess(this.Url);

            List<IO.Swagger.Model.Input> inputs = new List<IO.Swagger.Model.Input>();
            foreach (var dataInput in executeInput.DataInputs) {
                var inp = new Inputs();
                inp.Id = dataInput.Identifier.Value;

                context.LogDebug(this, "BuildExecute - input = " + dataInput.Identifier.Value);

                if (dataInput.Data != null && dataInput.Data.Item != null) {
                    if (dataInput.Data.Item is OpenGis.Wps.LiteralDataType) {

                        var datatype = "string";
                        foreach (var i in wps3.Process.Inputs) {
                            if (inp.Id == i.Id) {
                                if (i.Input.LiteralDataDomains != null) {
                                    var literaldomain = i.Input.LiteralDataDomains[0];
                                    if (!string.IsNullOrEmpty(literaldomain.DataType.Reference)) {
                                        datatype = literaldomain.DataType.Reference;
                                    }
                                }
                            }
                        }

                        var ld = dataInput.Data.Item as OpenGis.Wps.LiteralDataType;
                        string inputValue = ld.Value;
                        Uri outUri;
                        if (Uri.TryCreate(inputValue, UriKind.Absolute, out outUri) && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps)) {
                            try {
                                var urib = new UriBuilder(inputValue);
                                var nvc = HttpUtility.ParseQueryString(urib.Query);

                                //case WPS3 endpoint does not support format=json
                                if (CatalogueFactory.IsCatalogUrl(urib.Uri)
                                    && !string.IsNullOrEmpty(context.GetConfigValue("wps3input-format"))) {
                                    nvc["format"] = context.GetConfigValue("wps3input-format");
                                }
                                //case WPS3 endpoint needs a specific downloadorigin
                                if (CatalogueFactory.IsCatalogUrl(urib.Uri)
                                    && !string.IsNullOrEmpty(context.GetConfigValue("wps3input-downloadorigin"))) {
                                    nvc["do"] = context.GetConfigValue("wps3input-downloadorigin");
                                }
                                string[] queryString = Array.ConvertAll(nvc.AllKeys, key => string.Format("{0}={1}", key, nvc[key]));
                                urib.Query = string.Join("&", queryString);
                                inputValue = urib.Uri.AbsoluteUri;
                            } catch (System.Exception e) {
                                context.LogError(this, e.Message);                                
                            }
                        }

                        StupidData literalData = new StupidData(inputValue, new StupidDataType(datatype));
                        inputs.Add(new IO.Swagger.Model.Input(dataInput.Identifier.Value, literalData));
                    }
                }
            }

            List<IO.Swagger.Model.Output> outputs = new List<IO.Swagger.Model.Output>();
            var output = new IO.Swagger.Model.Output();
            output.Id = "wf_outputs";
            output.TransmissionMode = TransmissionMode.Reference;
            output.Format = new IO.Swagger.Model.Format("application/json");
            outputs.Add(output);

            return new IO.Swagger.Model.Execute(inputs, outputs, IO.Swagger.Model.Execute.ModeEnum.Async, IO.Swagger.Model.Execute.ResponseEnum.Raw, null);
        }

        /*******************/
        /* WPS 3.0 RESULTS */
        /*******************/

        public ResultOutputs GetOutputs(string jobStatusLocation) {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(jobStatusLocation + "/result");
            webRequest.Method = "GET";
            webRequest.Accept = "application/json";
            webRequest.ContentType = "application/json";


            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    var result = streamReader.ReadToEnd();
                    var response = ServiceStack.Text.JsonSerializer.DeserializeFromString<ResultOutputs>(result);
                    return response;
                }
            }
        }

        public override object GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
                case "correlatedTo":
                    var settings = MasterCatalogue.OpenSearchFactorySettings;
                    var urlBOS = new UrlBasedOpenSearchable(context, new OpenSearchUrl(value), settings);
                    var entity = urlBOS.Entity;
                    if (entity is EntityList<ThematicApplicationCached>) {
                        var entitylist = entity as EntityList<ThematicApplicationCached>;
                        var items = entitylist.GetItemsAsList();
                        if (items.Count > 0) {
                            var feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(items[0].TextFeed);
                            if (feed != null) {
                                var entry = feed.Items.First();
                                foreach (var offering in entry.Offerings) {
                                    switch (offering.Code) {
                                        case "http://www.opengis.net/spec/owc/1.0/req/atom/wps":
                                            if (offering.Operations != null && offering.Operations.Length > 0) {
                                                foreach (var operation in offering.Operations) {
                                                    var href = operation.Href;
                                                    switch (operation.Code) {
                                                        case "ListProcess":
                                                            var result = new List<KeyValuePair<string, string>>();
                                                            var uri = new Uri(href);
                                                            var nvc = HttpUtility.ParseQueryString(uri.Query);
                                                            foreach (var key in nvc.AllKeys) {
                                                                switch (key) {
                                                                    case "domain":
                                                                        if (nvc[key] != null) {
                                                                            string domainIdentifier = null;
                                                                            if (nvc[key].Contains("${USERNAME}")) {
                                                                                var user = UserTep.FromId(context, context.UserId);
                                                                                user.LoadCloudUsername();
                                                                                domainIdentifier = nvc[key].Replace("${USERNAME}", user.TerradueCloudUsername);
                                                                            } else domainIdentifier = nvc[key];
                                                                            if (!string.IsNullOrEmpty(domainIdentifier)) {
                                                                                var domain = Domain.FromIdentifier(context, domainIdentifier);
                                                                                result.Add(new KeyValuePair<string, string>("DomainId", domain.Id + ""));
                                                                            }
                                                                        }
                                                                        break;
                                                                    case "tag":
                                                                        if (!string.IsNullOrEmpty(nvc[key])) {
                                                                            var tags = nvc[key].Split(",".ToArray());
                                                                            IEnumerable<IEnumerable<string>> permutations = GetPermutations(tags, tags.Count());
                                                                            var r1 = permutations.Select(subset => string.Join("*", subset.Select(t => t).ToArray())).ToArray();
                                                                            var tagsresult = string.Join(",", r1.Select(t => "*" + t + "*"));
                                                                            result.Add(new KeyValuePair<string, string>("Tags", tagsresult));
                                                                        }
                                                                        break;
                                                                    default:
                                                                        break;
                                                                }
                                                            }
                                                            return result;
                                                        default:
                                                            break;
                                                    }
                                                }
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    return new KeyValuePair<string, string>("DomainId", "-1");//we don't want any result to be returned, as no service is returned to the app (no wps search link)
                default:
                    return base.GetFilterForParameter(parameter, value);
            }
        }

        public static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length) {
            if (length == 1) return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }
    }
}
