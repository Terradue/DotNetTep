using System;
using System.IO;
using System.Net;
using ServiceStack.Text;

namespace Terradue.Tep.Controller {
    public class JiraClient {

        public string APIBaseUrl { get; set; }
        public string APIUsername { get; set; }
        public string APIPassword { get; set; }

        public JiraClient() {
        }

        public void CreateUser(){
            //see https://docs.atlassian.com/software/jira/docs/api/REST/7.12.0/#api/2/user-createUser

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.APIBaseUrl + "/rest/api/2/user");
            request.Proxy = null;
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Credentials = new NetworkCredential(this.APIUsername, this.APIPassword);

            var issue = new JiraUserRequest {

            };

            string json = JsonSerializer.SerializeToString<JiraUserRequest>(issue);

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
        }

        public void CreateIssue(){
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.APIBaseUrl + "/rest/api/2/issue/createmeta");
            request.Proxy = null;
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Credentials = new NetworkCredential(this.APIUsername, this.APIPassword);

            string projectID = "TEST";
            string summary = "This is a test";
            string description = "This is a test description";
            string issueType = "Bug";

            var issue = new JiraIssueRequest { 
                fields = new JiraIssueFields {
                    project = new JiraIdProperty{ id = projectID },
                    summary = summary,
                    description = description,
                    issuetype = new JiraIdProperty {
                        id = issueType
                    }
                }
            };

            string json = JsonSerializer.SerializeToString<JiraIssueRequest>(issue);

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

        }
    }
}
