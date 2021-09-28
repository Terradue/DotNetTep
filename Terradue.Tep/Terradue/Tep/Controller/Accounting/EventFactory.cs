using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Terradue.Portal;

namespace Terradue.Tep {
    public class EventFactory {

        public static void Log(IfyContext context, Event log){

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["EVENT_LOG_URL"]);
            webRequest.Method = "POST";
            webRequest.Accept = "application/json";
            webRequest.ContentType = "application/json";

            // var json = ServiceStack.Text.JsonSerializer.SerializeToString<Event>(log);
            var json = JsonConvert.SerializeObject(log);
            context.LogDebug(context, "Event log : " + json);

            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream())) {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {}
            }
        }      

        public static string GetEventIdForWpsJob(WpsJobStatus status){
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
    }

    public class EventType {
        public const string JOB = "portal_job";
        public const string USER = "portal_user";
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
