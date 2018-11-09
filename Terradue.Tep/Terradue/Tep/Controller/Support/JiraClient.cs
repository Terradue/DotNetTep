using System;
using System.IO;
using System.Net;
using System.Text;
using ServiceStack.Text;

namespace Terradue.Tep {
    public class JiraClient {

        public string APIBaseUrl { get; set; }
        public string APIUsername { get; set; }
        public string APIPassword { get; set; }

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

        //public void CreateUser(){
        //    //see https://docs.atlassian.com/software/jira/docs/api/REST/7.12.0/#api/2/user-createUser

        //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.APIBaseUrl + "/rest/api/2/user");
        //    request.Proxy = null;
        //    request.Method = "POST";
        //    request.ContentType = "application/json";
        //    request.Accept = "application/json";
        //    request.Credentials = new NetworkCredential(this.APIUsername, this.APIPassword);

        //    var issue = new JiraUserRequest {

        //    };

        //    string json = JsonSerializer.SerializeToString<JiraUserRequest>(issue);

        //    using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
        //        streamWriter.Write(json);
        //        streamWriter.Flush();
        //        streamWriter.Close();

        //        using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
        //            using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
        //                string result = streamReader.ReadToEnd();
        //            }
        //        }
        //    }
        //}

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

            try {
                using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                            string result = streamReader.ReadToEnd();
                        }
                    }
                }
            } catch (Exception e) {
                throw e;
            }
        }
    }
}
