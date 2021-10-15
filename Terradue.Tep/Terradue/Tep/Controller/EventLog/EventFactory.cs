using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Xml;
using Newtonsoft.Json;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Terradue.Stars.Interface.Router.Translator;
using Terradue.Stars.Services.Translator;
using Terradue.Stars.Services.Credentials;
using Terradue.Stars.Data.Translators;
using System.Web;

namespace Terradue.Tep {
    public class EventFactory
    {
        public static EventLogConfiguration EventLogConfig = System.Configuration.ConfigurationManager.GetSection("EventLogConfiguration") as EventLogConfiguration;

        public static void Log(IfyContext context, Event log)
        {
            if (EventLogConfig == null || EventLogConfig.Settings == null || EventLogConfig.Settings.Count == 0 || string.IsNullOrEmpty(EventLogConfig.Settings["baseurl"].Value))
                throw new Exception("Missing event log configuration in web.config");

            var json = JsonConvert.SerializeObject(log);
            context.LogDebug(context, "Event log : " + json);
            
            // var settings = new ConnectionSettings(new Uri(EventLogConfig.Settings["baseurl"].Value));
            
            // if (EventLogConfig.Settings["auth_apikey"] != null && !string.IsNullOrEmpty(EventLogConfig.Settings["auth_apikey"].Value))
            //     settings.ApiKeyAuthentication(new Elasticsearch.Net.ApiKeyAuthenticationCredentials(EventLogConfig.Settings["auth_apikey"].Value));
            // else if (EventLogConfig.Settings["auth_username"] != null && !string.IsNullOrEmpty(EventLogConfig.Settings["auth_username"].Value) && EventLogConfig.Settings["auth_password"] != null)
            //     settings.BasicAuthentication(EventLogConfig.Settings["auth_username"].Value, EventLogConfig.Settings["auth_password"].Value);                

            // var client = new ElasticClient(settings);
            // var response = client.Index(log, e => e.Index(EventLogConfig.Settings["index"].Value).Pipeline(EventLogConfig.Settings["pipeline"].Value));

            var esUrib = new UriBuilder(EventLogConfig.Settings["baseurl"].Value);
            esUrib.Path = string.Format("{0}/_doc", EventLogConfig.Settings["index"].Value);
            if(EventLogConfig.Settings["pipeline"] != null && !string.IsNullOrEmpty(EventLogConfig.Settings["pipeline"].Value)) 
                esUrib.Query =  string.Format("pipeline={0}", EventLogConfig.Settings["pipeline"].Value);
            
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(esUrib.Uri.AbsoluteUri);
            webRequest.Method = "POST";
            webRequest.Accept = "application/json";
            webRequest.ContentType = "application/json";
            if (EventLogConfig.Settings["auth_apikey"] != null && !string.IsNullOrEmpty(EventLogConfig.Settings["auth_apikey"].Value))
                webRequest.Headers.Set(HttpRequestHeader.Authorization, "Bearer " + EventLogConfig.Settings["auth_apikey"].Value);
            else if (EventLogConfig.Settings["auth_username"] != null && !string.IsNullOrEmpty(EventLogConfig.Settings["auth_username"].Value) && EventLogConfig.Settings["auth_password"] != null)
                webRequest.Headers.Set(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(EventLogConfig.Settings["auth_username"].Value + ":" + EventLogConfig.Settings["auth_password"].Value)));

            try
            {
                using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) { }
                }
            }catch(Exception e){
                context.LogError(context, "Log event error (POST): " + e.Message);
            }

        }

        public static async System.Threading.Tasks.Task LogWpsJob(IfyContext context, WpsJob job, string message)
        {
            if (EventLogConfig == null || EventLogConfig.Settings == null || EventLogConfig.Settings.Count == 0) return;

            IEventJobLogger logger;            
            string className = EventLogConfig.Settings["job_classname"].Value;
            if (className == null)
            {
                logger = new EventJobLoggerTep(context);
            }
            else
            {
                Type type = Type.GetType(className, true);
                System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[] { typeof(IfyContext) });
                logger = (IEventJobLogger)ci.Invoke(new object[] { context });
            }

            if (logger != null){
                var logevent = await logger.GetLogEvent(job, message);
                Log(context, logevent);
            }
        }

        public static async System.Threading.Tasks.Task LogUser(IfyContext context, UserTep usr, string eventid, string message)
        {
            if (EventLogConfig == null || EventLogConfig.Settings == null || EventLogConfig.Settings.Count == 0) return;

            IEventUserLogger logger;
            string className = EventLogConfig.Settings["user_classname"].Value;
            if (className == null)
            {
                logger = new EventUserLoggerTep(context);
            }
            else
            {
                Type type = Type.GetType(className, true);
                System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[] { typeof(IfyContext) });
                logger = (IEventUserLogger)ci.Invoke(new object[] { context });
            }

            if (logger != null){
                var logevent = await logger.GetLogEvent(usr, eventid, message);
                Log(context, logevent);               
            }
        }
    }

    /*************************/
    /******** WPS JOB ********/
    /*************************/
    public interface IEventJobLogger {
        System.Threading.Tasks.Task<Event> GetLogEvent(WpsJob job, string message);
    }

    public class EventJobLoggerTep : IEventJobLogger {

        protected IfyContext context;
        public EventJobLoggerTep(IfyContext context){
            this.context = context;
        }

        protected string GetEventIdForWpsJob(WpsJobStatus status){
            switch (status) {
                case WpsJobStatus.NONE:
                    return "job_main_processing";
                case WpsJobStatus.ACCEPTED:
                    return "job_accepted_main_processing";
                case WpsJobStatus.STARTED:
                    return "job_start_main_processing";
                case WpsJobStatus.PAUSED:
                    return "job_pause_main_processing";
                case WpsJobStatus.SUCCEEDED:
                    return "job_end_main_processing";
                case WpsJobStatus.STAGED:
                    return "job_end_main_processing";
                case WpsJobStatus.FAILED:
                    return "job_end_main_processing";
                case WpsJobStatus.COORDINATOR:
                    return "job_coordinator_main_processing";
                default:
                    return null;
            }
        }  

        /// <summary>
        /// Create a job event (metrics)
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<Event> GetLogEvent(WpsJob job, string message = null){            
            try
            {
                var durations = new Dictionary<string, int>();
                durations.Add("from_start", ((int)(DateTime.UtcNow - job.CreatedTime).TotalSeconds));
                if (job.EndTime != DateTime.MinValue) durations.Add("from_end", ((int)(DateTime.UtcNow - job.EndTime).TotalSeconds));

                var properties = new Dictionary<string, object>();
                properties.Add("remote_identifier", job.RemoteIdentifier);
                properties.Add("wf_id", job.WpsName);
                properties.Add("wf_version", job.WpsVersion);
                properties.Add("app_id", job.AppIdentifier);
                properties.Add("status_url", job.StatusLocation);
                if (job.Owner != null) properties.Add("author", job.Owner.Username);
                if (job.Parameters != null)
                {
                    var token = DBCookie.LoadDBCookie(context, System.Configuration.ConfigurationManager.AppSettings["SUPERVISOR_COOKIE_TOKEN_ACCESS"]).Value;
                    var credentials = new NetworkCredential(job.Owner.Username, token);                    
                    var router = new Stars.Services.Model.Atom.AtomRouter(credentials);
                    ServiceCollection services = new ServiceCollection();
                    services.AddLogging(builder => builder.AddConsole());
                    services.AddTransient<ITranslator, StacLinkTranslator>();
                    services.AddTransient<ITranslator, AtomToStacTranslator>();
                    services.AddTransient<ITranslator, DefaultStacTranslator>();
                    services.AddTransient<ICredentials, ConfigurationCredentialsManager>();
                    var sp = services.BuildServiceProvider();

                    //add inputs
                    var paramDictionary = new Dictionary<string, object>();
                    var stacItemList = new List<object>();
                    foreach (var p in job.Parameters)
                    {
                        if (!paramDictionary.ContainsKey(p.Key)) paramDictionary.Add(p.Key, p.Value);
                        else
                        {
                            if (!(paramDictionary[p.Key] is List<string>)) paramDictionary[p.Key] = new List<string> { paramDictionary[p.Key] as string };
                            (paramDictionary[p.Key] as List<string>).Add(p.Value);
                        }

                        string osUrl = null;
                        try
                        {
                            if (!string.IsNullOrEmpty(p.Value))
                            {
                                var urib = new UriBuilder(p.Value);
                                if (!(urib.Host == new Uri(System.Configuration.ConfigurationManager.AppSettings["CatalogBaseUrl"]).Host)) continue;

                                var query = HttpUtility.ParseQueryString(urib.Query);
                                query.Set("format", "atom");
                                var queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
                                urib.Query = string.Join("&", queryString);
                                osUrl = urib.Uri.AbsoluteUri;
                            }
                        }
                        catch(Exception){}

                        // add data input stac item
                        if (!string.IsNullOrEmpty(osUrl))
                        {
                            try
                            {                           
                                var atomFeed = AtomFeed.Load(XmlReader.Create(osUrl));
                                var item = new Stars.Services.Model.Atom.AtomItemNode(atomFeed.Items.First() as AtomItem, new Uri(osUrl), credentials);
                                var translatorManager = new TranslatorManager(sp.GetService<ILogger<TranslatorManager>>(), sp);
                                var stacNode = translatorManager.Translate<Stars.Services.Model.Stac.StacItemNode>(item).GetAwaiter().GetResult();
                                stacItemList.Add(stacNode.StacItem);
                            }
                            catch (Exception e)
                            {
                                context.LogError(job, "Log event stac item error : " + osUrl + " - " + e.Message);
                            }
                        }
                    }
                    properties.Add("parameters", paramDictionary);
                    properties.Add("inputs", stacItemList);
                }

                var logevent = new Event
                {
                    EventId = GetEventIdForWpsJob(job.Status),
                    Project = EventFactory.EventLogConfig.Settings["project"].Value,
                    Status = job.StringStatus,
                    Durations = durations,
                    Transmitter = EventFactory.EventLogConfig.Settings["transmitter"].Value,
                    Message = message,
                    Item = new EventItem
                    {
                        Created = job.CreatedTime,
                        Updated = job.EndTime != DateTime.MinValue ? job.EndTime : job.CreatedTime,
                        Id = job.Identifier,
                        Title = job.Name,
                        Type = "portal_job",
                        Properties = properties
                    }
                };

                return logevent;
            }catch(Exception e){
                context.LogError(job, "Log event error: " + e.Message);
            }
            return null;
        }
    }

    /**********************/
    /******** USER ********/
    /**********************/
    public interface IEventUserLogger {
        System.Threading.Tasks.Task<Event> GetLogEvent(UserTep usr, string eventid, string message);
    }

    public class EventUserLoggerTep : IEventUserLogger {

        protected IfyContext context;
        public EventUserLoggerTep(IfyContext context){
            this.context = context;
        }

        /// <summary>
        /// Create a user event (metrics)
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<Event> GetLogEvent(UserTep usr, string eventid, string message = null){
            try
            {
                var properties = new Dictionary<string, object>();
                properties.Add("affiliation", usr.Affiliation);
                properties.Add("country", usr.Country);

                var logevent = new Event
                {
                    EventId = eventid,
                    Project = EventFactory.EventLogConfig.Settings["project"].Value,
                    Status = usr.Level.ToString(),
                    // Durations = durations,
                    Transmitter = EventFactory.EventLogConfig.Settings["transmitter"].Value,
                    Message = message,
                    Item = new EventItem
                    {
                        Created = usr.GetFirstLoginDate(),
                        Updated = usr.GetLastLoginDate(),
                        Id = usr.Username,
                        Title = usr.Caption,
                        Type = "portal_user",
                        Properties = properties
                    }
                };

                return logevent;
            }catch(Exception e){
                context.LogError(usr, "Log event error: " + e.Message);
            }
            return null;
        }
    }

    [DataContract]
    public class Event
    {
        [JsonProperty("event_id")]
        public string EventId { get; set; }

        [JsonProperty("item")]
        public EventItem Item { get; set; }

        [JsonProperty("durations")]
        public Dictionary<string,int> Durations { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("project")]
        public string Project { get; set; }

        [JsonProperty("transmitter")]
        public string Transmitter { get; set; }
    }

    [DataContract]
    public class EventItem
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("updated")]
        public DateTime Updated { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }

    [DataContract]
    public class EventProductMission
    {
        [JsonProperty("count")]
        public int count { get; set; }        
    }

}
