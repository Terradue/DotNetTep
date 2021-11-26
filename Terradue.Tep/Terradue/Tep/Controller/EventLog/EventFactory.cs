using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Terradue.Portal;
using Nest;
using Elasticsearch.Net;
using Nest.JsonNetSerializer;

namespace Terradue.Tep {
    public class EventFactory
    {
        public static EventLogConfiguration EventLogConfig = System.Configuration.ConfigurationManager.GetSection("EventLogConfiguration") as EventLogConfiguration;

        public static async System.Threading.Tasks.Task Log(IfyContext context, Event log)
        {
            if (EventLogConfig == null || EventLogConfig.Settings == null || EventLogConfig.Settings.Count == 0 || string.IsNullOrEmpty(EventLogConfig.Settings["baseurl"].Value))
                throw new Exception("Missing event log configuration in web.config");

            var json = JsonConvert.SerializeObject(log);
            context.LogDebug(context, "Event log : " + json);
                           
            var settings = new ConnectionSettings(new SingleNodeConnectionPool(new Uri(EventLogConfig.Settings["baseurl"].Value)), sourceSerializer: JsonNetSerializer.Default);
            
            if (EventLogConfig.Settings["auth_apikey"] != null && !string.IsNullOrEmpty(EventLogConfig.Settings["auth_apikey"].Value))
                settings.ApiKeyAuthentication(new Elasticsearch.Net.ApiKeyAuthenticationCredentials(EventLogConfig.Settings["auth_apikey"].Value));
            else if (EventLogConfig.Settings["auth_username"] != null && !string.IsNullOrEmpty(EventLogConfig.Settings["auth_username"].Value) && EventLogConfig.Settings["auth_password"] != null)
                settings.BasicAuthentication(EventLogConfig.Settings["auth_username"].Value, EventLogConfig.Settings["auth_password"].Value);                

            var client = new ElasticClient(settings);
            try{
                var response = await client.IndexAsync(log, e => e.Index(EventLogConfig.Settings["index"].Value).Pipeline(EventLogConfig.Settings["pipeline"].Value));
                context.LogDebug(context, string.Format("Log event response: (ID={0}) {1}", response.Id, response.DebugInformation));
            }catch(Exception e){
                context.LogError(context, "Log event error  (POST): " + e.Message);
                context.WriteError("Log event error  (POST): " + e.Message);
            }
        }

        public static async System.Threading.Tasks.Task LogWpsJob(IfyContext context, WpsJob job, string message)
        {
            if (EventLogConfig == null || EventLogConfig.Settings == null || EventLogConfig.Settings.Count == 0)
            {
                context.WriteError("Log error : missing configuration");
                context.LogError(context, "Log error : missing configuration");
                return;
            }

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
                await Log(context, logevent);
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
                await Log(context, logevent);               
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
                    return "portal_job_main_processing";
                case WpsJobStatus.ACCEPTED:
                    return "portal_job_accepted_main_processing";
                case WpsJobStatus.STARTED:
                    return "portal_job_start_main_processing";
                case WpsJobStatus.PAUSED:
                    return "portal_job_pause_main_processing";
                case WpsJobStatus.SUCCEEDED:
                    return "portal_job_end_main_processing";
                case WpsJobStatus.STAGED:
                    return "portal_job_end_main_processing";
                case WpsJobStatus.FAILED:
                    return "portal_job_end_main_processing";
                case WpsJobStatus.COORDINATOR:
                    return "portal_job_coordinator_main_processing";
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

                var properties = GetJobBasicProperties(job);
                var stacItems = job.GetJobInputsStacItems();
                properties.Add("stac_items", stacItems);

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

        protected Dictionary<string, object> GetJobBasicProperties(WpsJob job)
        {
            var properties = new Dictionary<string, object>();
            properties.Add("remote_identifier", job.RemoteIdentifier);
            properties.Add("wf_id", job.WpsName);
            properties.Add("wf_version", job.WpsVersion);
            properties.Add("app_id", job.AppIdentifier);
            properties.Add("status_url", job.StatusLocation);
            if (job.Owner != null)
            {
                var author = new Dictionary<string, string>();
                author.Add("username", job.Owner.Username);
                if(!string.IsNullOrEmpty(job.Owner.Affiliation)) author.Add("affiliation", job.Owner.Affiliation);
                if(!string.IsNullOrEmpty(job.Owner.Country)) author.Add("country", job.Owner.Country);
                properties.Add("author", author);
            }
            var inputProperties = new Dictionary<string, object>();
            foreach (var p in job.Parameters)
            {
                if (!inputProperties.ContainsKey(p.Key)) 
                    inputProperties.Add(p.Key, p.Value);
                else
                {
                    if (!(inputProperties[p.Key] is List<string>)) inputProperties[p.Key] = new List<string> { inputProperties[p.Key] as string };
                    (inputProperties[p.Key] as List<string>).Add(p.Value);
                }
            }
            properties.Add("parameters", inputProperties);
            return properties;
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

    public class EventProductMission
    {
        [JsonProperty("count")]
        public int count { get; set; }        
    }

}
