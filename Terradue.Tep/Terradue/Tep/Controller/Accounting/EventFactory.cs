using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Terradue.Portal;

namespace Terradue.Tep {
    public class EventFactory
    {

        public static void Log(IfyContext context, Event log)
        {
            if (string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["EVENT_LOG_URL"])) return;

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["EVENT_LOG_URL"]);
            webRequest.Method = "POST";
            webRequest.Accept = "application/json";
            webRequest.ContentType = "application/json";

            var json = JsonConvert.SerializeObject(log);
            context.LogDebug(context, "Event log : " + json);

            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) { }
            }
        }

        public static void LogWpsJob(IfyContext context, WpsJob job, string message){
            IEventJobLogger logger;
            string className = System.Configuration.ConfigurationManager.AppSettings["EVENT_LOG_JOB_CLASSNAME"];
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
                var logevent = logger.GetLogEvent(job, message);
                Log(context, logevent);
            }
        }

        public static void LogUser(IfyContext context, UserTep usr, string eventid, string message){
            IEventUserLogger logger;
            string className = System.Configuration.ConfigurationManager.AppSettings["EVENT_LOG_USER_CLASSNAME"];
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
                var logevent = logger.GetLogEvent(usr, eventid, message);
                Log(context, logevent);
            }
        }
    }

    /*************************/
    /******** WPS JOB ********/
    /*************************/
    public interface IEventJobLogger {
        Event GetLogEvent(WpsJob job, string message);
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
        public Event GetLogEvent(WpsJob job, string message = null){
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
                    var paramDictionary = new Dictionary<string, object>();
                    foreach (var p in job.Parameters)
                    {
                        if (!paramDictionary.ContainsKey(p.Key)) paramDictionary.Add(p.Key, p.Value);
                        else
                        {
                            if (!(paramDictionary[p.Key] is List<string>)) paramDictionary[p.Key] = new List<string> { paramDictionary[p.Key] as string };
                            (paramDictionary[p.Key] as List<string>).Add(p.Value);
                        }
                    }
                    properties.Add("inputs", paramDictionary);
                }

                var logevent = new Event
                {
                    EventId = GetEventIdForWpsJob(job.Status),
                    Timestamp = DateTime.UtcNow,
                    Project = context.GetConfigValue("SiteNameShort"),
                    Status = job.StringStatus,
                    Durations = durations,
                    Transmitter = System.Configuration.ConfigurationManager.AppSettings["EVENT_LOG_TRANSMITTER"],
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
        Event GetLogEvent(UserTep usr, string eventid, string message);
    }

    public class EventUserLoggerTep : IEventUserLogger {

        protected IfyContext context;
        public EventUserLoggerTep(IfyContext context){
            this.context = context;
        }

        /// <summary>
        /// Create a job event (metrics)
        /// </summary>
        /// <returns></returns>
        public Event GetLogEvent(UserTep usr, string eventid, string message = null){
            try
            {
                var properties = new Dictionary<string, object>();
                properties.Add("affiliation", usr.Affiliation);
                properties.Add("country", usr.Country);

                var logevent = new Event
                {
                    EventId = eventid,
                    Timestamp = DateTime.UtcNow,
                    Project = context.GetConfigValue("SiteNameShort"),
                    Status = usr.Level.ToString(),
                    // Durations = durations,
                    Transmitter = System.Configuration.ConfigurationManager.AppSettings["EVENT_LOG_TRANSMITTER"],
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

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

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

        [DataMember(Name = "properties")]
        public Dictionary<string, object> Properties { get; set; }
    }
}
