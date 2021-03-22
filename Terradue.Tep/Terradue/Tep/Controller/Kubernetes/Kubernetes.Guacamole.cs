using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Terradue.Tep.Controller.Kubernetes.Guacamole {
    [DataContract]
    public class GuacamoleTokenResponse {
        [DataMember]
        public string authToken { get; set; }
        [DataMember]
        public string username { get; set; }
        [DataMember]
        public string dataSource { get; set; }
        [DataMember]
        public string role { get; set; }
        [DataMember]
        public List<string> availableDataSources { get; set; }
    }

    [DataContract]
    public class GuacamoleUser {
        [DataMember]
        public string username { get; set; }
        [DataMember]
        public GuacamoleUserAttributes attributes { get; set; }
    }

    [DataContract]
    public class GuacamoleUserAttributes {
        [DataMember]
        public string disabled { get; set; }
        [DataMember]
        public string expired { get; set; }
        [DataMember(Name = "access-window-start")]
        public string access_window_start { get; set; }
        [DataMember(Name = "access-window-end")]
        public string access_window_end { get; set; }
        [DataMember(Name = "valid-from")]
        public string valid_from { get; set; }
        [DataMember(Name = "valid-until")]
        public string valid_until { get; set; }
        [DataMember]
        public string timezone { get; set; }
    }

    [DataContract]
    public class GuacamoleConnection {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public int identifier { get; set; }
        [DataMember]
        public string parentIdentifier { get; set; }
        [DataMember]
        public string protocol { get; set; }
        [DataMember]
        public GuacamoleParameters parameters { get; set; }
        [DataMember]
        public GuacamoleAttributes attributes { get; set; }
        [DataMember]
        public int activeConnections { get; set; }
    }

    [DataContract]
    public class GuacamoleParameters {
        [DataMember]
        public string port { get; set; }
        [DataMember(Name = "read-only")]
        public string read_only { get; set; }
        [DataMember(Name = "swap-red-blue")]
        public string swap_red_blue { get; set; }
        [DataMember]
        public string cursor { get; set; }
        [DataMember(Name = "color-depth")]
        public string color_depth { get; set; }
        [DataMember(Name = "clipboard-encoding")]
        public string clipboard_encoding { get; set; }
        [DataMember(Name = "dest-port")]
        public string dest_port { get; set; }
        [DataMember(Name = "recording-exclude-output")]
        public string recording_exclude_output { get; set; }
        [DataMember(Name = "recording-exclude-mouse")]
        public string recording_exclude_mouse { get; set; }
        [DataMember(Name = "recording-include-keys")]
        public string recording_include_keys { get; set; }
        [DataMember(Name = "create-recording-path")]
        public string create_recording_path { get; set; }
        [DataMember(Name = "enable-sftp")]
        public string enable_sftp { get; set; }
        [DataMember(Name = "sftp-port")]
        public string sftp_port { get; set; }
        [DataMember(Name = "sftp-server-alive-interval")]
        public string sftp_server_alive_interval { get; set; }
        [DataMember(Name = "enable-audio")]
        public string enable_audio { get; set; }
        [DataMember]
        public string hostname { get; set; }
        [DataMember]
        public string password { get; set; }
        [DataMember]
        public string username { get; set; }
    }

    [DataContract]
    public class GuacamoleAttributes {
        [DataMember(Name = "failover-only")]
        public string failover_only { get; set; }
        [DataMember(Name = "guacd-encryption")]
        public string guacd_encryption { get; set; }
        [DataMember]
        public string weight { get; set; }
        [DataMember(Name = "max-connections")]
        public string max_connections { get; set; }
        [DataMember(Name = "guacd-hostname")]
        public string guacd_hostname { get; set; }
        [DataMember(Name = "guacd-port")]
        public string guacd_port { get; set; }
        [DataMember(Name = "max-connections-per-user")]
        public string max_connections_per_user { get; set; }
    }

    [DataContract]
    public class GuacamoleUserConnectionPermission {
        [DataMember]
        public string op { get; set; }
        [DataMember]
        public string path { get; set; }
        [DataMember]
        public string value { get; set; }
    }

    [DataContract]
    public class GuacamoleUserPermission {
        [DataMember]
        public Dictionary<int, List<string>> connectionPermissions { get; set; }
        [DataMember]
        public Dictionary<string, List<string>> userPermissions { get; set; }
    }
}
