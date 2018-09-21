using System;
using System.Runtime.Serialization;

namespace Terradue.Tep.Controller {

    [DataContract]
    public class JiraIssueProject {
        [DataMember]
        public string key { get; set; }
    }

    [DataContract]
    public class JiraIssuetype {
        [DataMember]
        public string name { get; set; }
    }

    [DataContract]
    public class JiraIssueFields {
        [DataMember]
        public JiraIssueProject project { get; set; }
        [DataMember]
        public string summary { get; set; }
        [DataMember]
        public string description { get; set; }
        [DataMember]
        public JiraIssuetype issuetype { get; set; }
    }

    [DataContract]
    public class JiraIssueRequest {
        [DataMember]
        public JiraIssueFields fields { get; set; }
    }

}
