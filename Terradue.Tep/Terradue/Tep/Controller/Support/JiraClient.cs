using System;
using System.IO;
using System.Net;
using System.Text;
using ServiceStack.Text;
using Terradue.Portal;

namespace Terradue.Tep {
    public class JiraClient {

        public string APIBaseUrl { get; set; }
        public string APIUsername { get; set; }
        public string APIPassword { get; set; }

        public string CustomField_ThematicAppLabel { get; set; }
        public string CustomField_ProcessingServiceLabel { get; set; }

        public JiraClient(IfyContext context){
            this.APIBaseUrl = context.GetConfigValue("jira-api-baseurl");
            this.APIUsername = context.GetConfigValue("jira-api-username");
            this.APIPassword = context.GetConfigValue("jira-api-password");
            this.CustomField_ThematicAppLabel = context.GetConfigValue("jira-helpdesk-customfield-ThematicAppLabel");
            this.CustomField_ProcessingServiceLabel = context.GetConfigValue("jira-helpdesk-customfield-ProcessingServiceLabel");
        }

        public JiraClient(string apibaseurl, string apiusername, string apipassword) {
            this.APIBaseUrl = apibaseurl;
            this.APIUsername = apiusername;
            this.APIPassword = apipassword;
        }

        private string GetAuthorizationHeader() {
            if (!string.IsNullOrEmpty(this.APIUsername))
                return "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(this.APIUsername + ":" + this.APIPassword));
            else return "Bearer " + this.APIPassword;
        }

        /// <summary>
        /// Creates the service desk issue.
        /// </summary>
        /// <param name="serviceDeskId">Service desk identifier.</param>
        /// <param name="requestTypeId">Request type identifier.</param>
        /// <param name="title">Title.</param>
        /// <param name="description">Description.</param>
        public void CreateServiceDeskIssue(JiraServiceDeskIssueRequest issue) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.APIBaseUrl + "/rest/servicedeskapi/request");
            request.Proxy = null;
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers.Add(HttpRequestHeader.Authorization, GetAuthorizationHeader());

            string json = JsonSerializer.SerializeToString<JiraServiceDeskIssueRequest>(issue);
            //we need to replace with customField_.... for the REST API
            if (!string.IsNullOrEmpty(this.CustomField_ThematicAppLabel)) json = json.Replace("\"thematicAppLabels\"", "\"" + CustomField_ThematicAppLabel + "\"");
            if (!string.IsNullOrEmpty(this.CustomField_ProcessingServiceLabel)) json = json.Replace("\"processingServicesLabels\"", "\"" + CustomField_ProcessingServiceLabel + "\"");

            try {
                using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,request.EndGetResponse,null)
                    .ContinueWith(task =>
                    {
                        var httpResponse = (HttpWebResponse)task.Result;                        
                    }).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            } catch (Exception e) {
                throw e;
            }
        }
    }
}
