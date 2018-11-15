using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Terradue.Tep {

    /*********************/
    /* JIRA SERVICE DESK */
    /*********************/

    [DataContract]
    public class JiraServiceDeskIssueFields {
        [DataMember]
        public string summary { get; set; }
        [DataMember]
        public string description { get; set; }
        [DataMember]
        public List<JiraNameProperty> components { get; set; }
        [DataMember]
        public List<string> labels { get; set; }
        [DataMember]
        public List<string> thematicAppLabels { get; set; }
        [DataMember]
        public List<string> processingServicesLabels { get; set; }
    }

    [DataContract]
    public class JiraServiceDeskIssueRequest {
        [DataMember]
        public string serviceDeskId { get; set; }
        [DataMember]
        public string requestTypeId { get; set; }
        [DataMember]
        public string raiseOnBehalfOf { get; set; }
        [DataMember]
        public JiraServiceDeskIssueFields requestFieldValues { get; set; }
        [DataMember]
        public List<string> larequestParticipantsbels { get; set; }
    }

    /********/
    /* JIRA */
    /********/

    [DataContract]
    public class JiraIdProperty {
        [DataMember]
        public string id { get; set; }
    }

    [DataContract]
    public class JiraNameProperty {
        [DataMember]
        public string name { get; set; }
    }

    [DataContract]
    public class JiraIssueFields {
        [DataMember]
        public JiraIdProperty project { get; set; }
        [DataMember]
        public string summary { get; set; }
        [DataMember]
        public string description { get; set; }
        [DataMember]
        public JiraIdProperty issuetype { get; set; }
        [DataMember]
        public JiraNameProperty assignee { get; set; }
        [DataMember]
        public JiraNameProperty reporter { get; set; }
        [DataMember]
        public JiraIdProperty priority { get; set; }
        [DataMember]
        public List<string> labels { get; set; }
        [DataMember]
        public DateTime duedate { get; set; }
    }

    [DataContract]
    public class JiraIssueRequest {
        [DataMember]
        public JiraIssueFields fields { get; set; }
    }

    [DataContract]
    public class JiraUserRequest {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string password { get; set; }
        [DataMember]
        public string emailAddress { get; set; }
        [DataMember]
        public string displayName { get; set; }
        [DataMember]
        public List<string> applicationKeys { get; set; }
    }
}
