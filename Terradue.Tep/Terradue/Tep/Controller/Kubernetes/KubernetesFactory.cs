using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Text;
using Terradue.Tep.Controller.Kubernetes.Kubectl;
using Terradue.Tep.Controller.Kubernetes.Guacamole;
using Terradue.Portal;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Terradue.Tep.Controller {
    public class KubernetesFactory {

        public System.Collections.Specialized.NameValueCollection AppSettings = System.Configuration.ConfigurationManager.AppSettings;

        public IfyContext Context;

        private string authtoken;
        public string AuthToken {
            get {
                if (string.IsNullOrEmpty(authtoken))
                    authtoken = GetAuthToken();
                return authtoken;
            }
        }

        public string K8S_APPNAME = "ubuntu-qgis-vnc";
        public string K8S_PVCNAME = "pvc-qgis";

        public KubernetesFactory(IfyContext context) {
            this.Context = context;
            if (!string.IsNullOrEmpty(AppSettings["K8S_APPNAME"])) K8S_APPNAME = AppSettings["K8S_APPNAME"];
            if (!string.IsNullOrEmpty(AppSettings["K8S_PVCNAME"])) K8S_PVCNAME = AppSettings["K8S_PVCNAME"];
        }

        /*********************/
        /****** KUBECTL ******/
        /*********************/

        /// <summary>
        /// Generate Kubectl PVC request object
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private KubectlPod GenerateKubectlPVCRequest(string username) {
            using (StreamReader reader = new StreamReader(AppSettings["K8S_PVC_YAML"])) {
                string pvcstring = reader.ReadToEnd().Replace("$(PODNAME)", username);
                var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                var requestPersistentStorage = deserializer.Deserialize<KubectlPod>(pvcstring);
                return requestPersistentStorage;
            };
        }

        /// <summary>
        /// Create user QGIS environment
        /// </summary>
        /// <param name="username"></param>
        /// <param name="volumeMounts"></param>
        /// <param name="volumes"></param>
        /// <returns></returns>
        public KubectlPod CreateUserQgisEnvironment(string username, KubectlPod k8srequest) {
            //pvc
            var pvc = GetPvc(username);
            if (pvc == null) pvc = CreatePvc(username);

            //pod
            var pod = GetPod(username);
            if (pod == null) pod = CreateDeployment(username, k8srequest);

            int i = 0;
            int maxtry = int.Parse(AppSettings["K8S_DESCRIBE_MAX_TRY"]);
            int sleep = int.Parse(AppSettings["K8S_DESCRIBE_SLEEP_MS"]);
            string pvcstatus = pvc != null && pvc.status != null ? pvc.status.phase : K8sPodStatus.Pending;
            string podstatus = pod != null && pod.status != null ? pod.status.phase : K8sPodStatus.Pending;
            while (podstatus != K8sPodStatus.Running && pvcstatus != K8sPodStatus.Running && i++ <= maxtry) {
                if (pvcstatus != K8sPodStatus.Running) {
                    pvc = GetPvc(username);
                    if (pvc != null && pvc.status != null) pvcstatus = pvc.status.phase;
                }
                if (podstatus != K8sPodStatus.Running) {
                    pod = GetPod(username);
                    if (pod != null && pod.status != null) podstatus = pod.status.phase;
                }
                System.Threading.Thread.Sleep(sleep);
            }
            return pod;
        }

        /// <summary>
        /// Delete user environment (not the persistens storage)
        /// </summary>
        /// <param name="username"></param>
        public void DeleteUserEnvironment(string username) {
            var podname = GenerateKubectlName(username);
            DeleteDeployment(podname);
        }

        /// <summary>
        /// Generates the K8S pvc name
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string GenerateKubectlPVCName(string username) {
            return string.Format("{0}-{1}", K8S_PVCNAME, username);
        }

        /// <summary>
        /// Generates the K8S name
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string GenerateKubectlName(string username) {
            return string.Format("{0}-{1}", K8S_APPNAME, username);
        }

        /// <summary>
        /// Create a new Persistent Volume Claim
        /// </summary>
        /// <param name="username"></param>
        private KubectlPod CreatePvc(string username) {

            var k8srequest = GenerateKubectlPVCRequest(username);

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(AppSettings["K8S_API_URL_PVC"]);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/json";
            webRequest.Headers["Authorization"] = "Bearer " + AppSettings["K8S_API_TOKEN"];

            string json = JsonSerializer.SerializeToString<KubectlPod>(k8srequest);

            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream())) {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        var response = JsonSerializer.DeserializeFromString<KubectlPod>(result);
                        return response;
                    }
                }
            }
        }

        /// <summary>
        /// Generate Kubectl request object
        /// </summary>
        /// <param name="username"></param>
        /// <param name="volumeMounts"></param>
        /// <param name="volumes"></param>
        /// <returns></returns>
        public KubectlPod GenerateKubectlRequest(string username, List<KubectlVolumeMount> volumeMounts, List<KubectlVolume> volumes) {

            using (StreamReader reader = new StreamReader(AppSettings["K8S_YAML"])) {
                string k8string = reader.ReadToEnd().Replace("$(PODNAME)", username);                
                var deserializer = new DeserializerBuilder().Build();
                var request = deserializer.Deserialize<KubectlPod>(k8string);

                //add volumes mount
                if (request.spec.template.spec.containers != null && request.spec.template.spec.containers.Count > 0) {
                    if (request.spec.template.spec.containers[0].volumeMounts == null) request.spec.template.spec.containers[0].volumeMounts = new List<KubectlVolumeMount>();
                    foreach (var volume in volumeMounts) request.spec.template.spec.containers[0].volumeMounts.Add(volume);
                }

                //add volumes
                if (request.spec.template.spec.volumes == null) request.spec.template.spec.volumes = new List<KubectlVolume>();
                foreach (var volume in volumes) request.spec.template.spec.volumes.Add(volume);
                
                return request;
            };
        }

        /// <summary>
        /// Get PVC for user
        /// </summary>
        /// <param name="username"></param>
        private KubectlPod GetPvc(string username) {

            var k8sPVC = GenerateKubectlPVCName(username);

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(AppSettings["K8S_API_URL_PVC"] + "/" + k8sPVC);
            webRequest.Method = "GET";
            webRequest.ContentType = "application/json";
            webRequest.Headers["Authorization"] = "Bearer " + AppSettings["K8S_API_TOKEN"];

            try {
                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        var response = JsonSerializer.DeserializeFromString<KubectlPod>(result);
                        return response;
                    }
                }
            } catch (Exception e) {
                return null;
            }
        }

        /// <summary>
        /// Create a new Deployment
        /// </summary>
        /// <param name="username"></param>
        /// <param name="volumeMounts"></param>
        /// <param name="volumes"></param>
        /// <returns></returns>
        private KubectlPod CreateDeployment(string username, KubectlPod k8srequest) {

            KubectlPod response = null;

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(AppSettings["K8S_API_URL_DEPL"]);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/json";
            webRequest.Headers["Authorization"] = "Bearer " + AppSettings["K8S_API_TOKEN"];

            string json = JsonSerializer.SerializeToString<KubectlPod>(k8srequest);

            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream())) {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        response = JsonSerializer.DeserializeFromString<KubectlPod>(result);
                        return response;
                    }
                }
            }
        }

        /// <summary>
        /// Delete the Pod
        /// </summary>
        /// <param name="podname"></param>
        private void DeleteDeployment(string podname) {

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(AppSettings["K8S_API_URL_DEPL"] + "/" + podname);
            webRequest.Method = "DELETE";
            webRequest.Headers["Authorization"] = "Bearer " + AppSettings["K8S_API_TOKEN"];

            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    string result = streamReader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Get the list of existing pods
        /// </summary>
        /// <param name="podname"></param>
        /// <returns></returns>
        private KubectlPods GetPods() {

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(AppSettings["K8S_API_URL_PODS"]);
            webRequest.Method = "GET";
            webRequest.ContentType = "application/json";
            webRequest.Headers["Authorization"] = "Bearer " + AppSettings["K8S_API_TOKEN"];

            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    string result = streamReader.ReadToEnd();
                    var response = JsonSerializer.DeserializeFromString<KubectlPods>(result);
                    return response;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public KubectlPod GetPod(string username) {

            var podname = GenerateKubectlName(username);
            var pods = GetPods();
            if (pods.items != null) {
                foreach (var item in pods.items) {
                    if (item.Identifier != null
                        && item.Identifier.Contains(podname))
                        return item;
                }
            }
            return null;
        }

        /***********************/
        /****** GUACAMOLE ******/
        /***********************/


        /// <summary>
        /// Get Guacamole Base url
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetGuacamoleBaseUrl(string path) {
            return string.Format("{0}{1}{2}",
                AppSettings["GUACAMOLE_BASEURL"],
                string.IsNullOrEmpty(AppSettings["GUACAMOLE_PORT"]) ? "" : ":" + AppSettings["GUACAMOLE_PORT"],
                path);
        }

        /// <summary>
        /// Get Guacamole API url
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetGuacamoleApiUrl(string path) {
            return string.Format("{0}{1}{2}",
                AppSettings["GUACAMOLE_API_URL"],
                string.IsNullOrEmpty(AppSettings["GUACAMOLE_PORT"]) ? "" : ":" + AppSettings["GUACAMOLE_PORT"],
                path);
        }

        /// <summary>
        /// Get Authentication token
        /// </summary>
        /// <returns></returns>
        private string GetAuthToken() {
            string token = null;

            var url = GetGuacamoleApiUrl("/api/tokens");
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "POST";
            //if (!string.IsNullOrEmpty(AppSettings["ProxyHost"])) webRequest.Proxy = GetWebRequestProxy();
            webRequest.ContentType = "application/x-www-form-urlencoded";

            var jsonDataString = string.Format("username={0}&password={1}", AppSettings["GUACAMOLE_USER"], AppSettings["GUACAMOLE_PASSWORD"]);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonDataString);
            webRequest.ContentLength = data.Length;

            using (var requestStream = webRequest.GetRequestStream()) {
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        var result = streamReader.ReadToEnd();
                        var response = JsonSerializer.DeserializeFromString<GuacamoleTokenResponse>(result);
                        token = response.authToken;
                    }
                }
            }
            return token;
        }

        /// <summary>
        /// Create user on the guacamole DB
        /// </summary>
        /// <param name="username"></param>
        public void CreateUserOnGuacamole(string username) {

            var url = GetGuacamoleApiUrl(string.Format("/api/session/data/postgresql/users?token={0}", AuthToken));
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/json";

            var userGuacamole = new GuacamoleUser { username = username, attributes = new GuacamoleUserAttributes() };
            string json = JsonSerializer.SerializeToString<GuacamoleUser>(userGuacamole);

            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream())) {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                    }
                }
            }
        }

        /// <summary>
        /// Delete user on Guacamole
        /// </summary>
        /// <param name="username"></param>
        public void DeleteUserOnGuacamole(string username) {

            var url = GetGuacamoleApiUrl(string.Format("/api/session/data/postgresql/users/{0}?token={1}", username, AuthToken));
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "DELETE";

            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    string result = streamReader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Check if user already on guacamole
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool IsUserOnGuacamole(string username) {

            var url = GetGuacamoleApiUrl(string.Format("/api/session/data/postgresql/users/{0}?token={1}", username, AuthToken));
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";

            try {
                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                    if (httpResponse.StatusCode == HttpStatusCode.OK) return true;
                    else return false;
                }
            } catch (Exception e) {
                return false;
            }

        }

        /// <summary>
        /// Get all Guacamole connections
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, GuacamoleConnection> GetGuacamoleConnections() {

            var url = GetGuacamoleApiUrl(string.Format("/api/session/data/postgresql/connections?token={0}", AuthToken));
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";

            try {
                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        return JsonSerializer.DeserializeFromString<Dictionary<string, GuacamoleConnection>>(result);
                    }
                }
            } catch (Exception e) {
                return null;
            }
        }

        /// <summary>
        /// Check if pod already on guacamole
        /// </summary>
        /// <param name="podName"></param>
        /// <returns></returns>
        public bool IsPodOnGuacamole(string podName) {

            var connections = GetGuacamoleConnections();
            foreach (var key in connections.Keys) {
                var conn = connections[key];
                if (conn.name == podName) return true;
            }
            return false;
        }

        /// <summary>
        /// Get Guacamole connection
        /// </summary>
        /// <param name="podName"></param>
        /// <returns></returns>
        public GuacamoleConnection GetGuacamoleConnection(string podName) {

            var connections = GetGuacamoleConnections();
            foreach (var key in connections.Keys) {
                var conn = connections[key];
                if (conn.name == podName) return conn;
            }
            return null;
        }

        /// <summary>
        /// Get Guacamole connection
        /// </summary>
        /// <param name="podID"></param>
        /// <returns></returns>
        public GuacamoleConnection GetGuacamoleConnection(int podID) {

            var url = GetGuacamoleApiUrl(string.Format("/api/session/data/postgresql/connections/{0}?token={1}", podID, AuthToken));
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";

            try {
                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        return JsonSerializer.DeserializeFromString<GuacamoleConnection>(result);
                    }
                }
            } catch (Exception e) {
                return null;
            }
        }

        /// <summary>
        /// Check if connection exists
        /// </summary>
        /// <param name="podID"></param>
        /// <returns></returns>
        public bool ExistsGuacamoleConnection(int podID) {
            var conn = GetGuacamoleConnection(podID);
            return conn != null;
        }

        /// <summary>
        /// Get Guacamole connection ID
        /// </summary>
        /// <param name="podName"></param>
        /// <returns></returns>
        public int GetGuacamoleConnectionID(string podName) {
            var conn = GetGuacamoleConnection(podName);
            if (conn != null) return conn.identifier;
            return 0;
        }

        /// <summary>
        /// Create VNC connection on guacamole
        /// Add Pod IP on guacamole DB
        /// </summary>
        /// <param name="podName"></param>
        /// <param name="podIP"></param>
        public int CreateGuacamoleVncConnection(string username, string podName, string podIP) {

            var url = GetGuacamoleApiUrl(string.Format("/api/session/data/postgresql/connections?token={0}", AuthToken));
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/json";

            var connGuacamole = new GuacamoleConnection {
                name = podName,
                parentIdentifier = "ROOT",
                protocol = AppSettings["GUACAMOLE_CONN_PROTOCOLE_VNC"],
                activeConnections = 0,
                parameters = new GuacamoleParameters { port = AppSettings["GUACAMOLE_CONN_PORT"], username = username, hostname = podIP, password = AppSettings["GUACAMOLE_VNC_PASSWORD"] },
                attributes = new GuacamoleAttributes()
            };
            string json = JsonSerializer.SerializeToString<GuacamoleConnection>(connGuacamole);

            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream())) {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        var response = JsonSerializer.DeserializeFromString<GuacamoleConnection>(result);
                        return response.identifier;
                    }
                }
            }
        }


        /// <summary>
        /// Delete connection on guacamole
        /// </summary>
        /// <param name="podID"></param>
        public void DeleteGuacamoleConnection(int podID) {

            var url = GetGuacamoleApiUrl(string.Format("/api/session/data/postgresql/connections/{0}?token={1}", podID, AuthToken));
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "DELETE";

            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    string result = streamReader.ReadToEnd();
                }
            }

        }

        /// <summary>
        /// Get user permissions
        /// </summary>
        /// <param name="username"></param>
        public GuacamoleUserPermission GetUserPermissions(string username) {

            var url = GetGuacamoleApiUrl(string.Format("/api/session/data/postgresql/users/{0}/permissions?token={1}", username, AuthToken));
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            webRequest.ContentType = "application/json";

            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    string result = streamReader.ReadToEnd();
                    var response = JsonSerializer.DeserializeFromString<GuacamoleUserPermission>(result);
                    return response;
                }
            }
        }

        /// <summary>
        /// Check if user connection permission already exists
        /// </summary>
        /// <param name="podID"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool ExistsGuacamoleUserConnectionPermission(int podID, string username) {
            var permissions = GetUserPermissions(username);
            if (permissions.connectionPermissions == null || permissions.connectionPermissions.Keys.Count == 0)
                return false;
            foreach (var key in permissions.connectionPermissions.Keys) {
                if (key == podID)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Add Connection permission to user
        /// </summary>
        /// <param name="podID"></param>
        /// <param name="username"></param>
        public void AddConnectionPermission(int podID, string username) {
            UpdateConnectionPermission(podID, username, "add");
        }

        /// <summary>
        /// Revoke Connection permission to user
        /// </summary>
        /// <param name="podID"></param>
        /// <param name="username"></param>
        public void RevokeConnectionPermission(int podID, string username) {
            UpdateConnectionPermission(podID, username, "remove");
        }

        /// <summary>
        /// Update Connection permission to user
        /// </summary>
        /// <param name="podID"></param>
        /// <param name="username"></param>
        public void UpdateConnectionPermission(int podID, string username, string op) {

            var url = GetGuacamoleApiUrl(string.Format("/api/session/data/postgresql/users/{0}/permissions?token={1}", username, AuthToken));
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "PATCH";
            webRequest.ContentType = "application/json";

            var permGuacamole = new List<GuacamoleUserConnectionPermission>();
            permGuacamole.Add(new GuacamoleUserConnectionPermission {
                op = op,
                path = "/connectionPermissions/" + podID,
                value = "READ"
            });
            string json = JsonSerializer.SerializeToString<List<GuacamoleUserConnectionPermission>>(permGuacamole);

            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream())) {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                using (var httpResponse = (HttpWebResponse)webRequest.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                    }
                }
            }
        }

        /// <summary>
        /// Get Guacamole url
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public string GetGuacamoleVncUrl(int podID) {
            var vnctoken = podID + "\0c\0" + AppSettings["GUACAMOLE_VNC_TOKEN_SUFFIX"];
            var vnctokenBytes = System.Text.Encoding.UTF8.GetBytes(vnctoken);
            var vncToken64 = System.Convert.ToBase64String(vnctokenBytes);

            return GetGuacamoleBaseUrl(string.Format("/#/client/{0}", HttpUtility.UrlEncode(vncToken64)));
        }


        /******/
        /* DB */
        /******/

        /// <summary>
        /// Get VNC id for user
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userid"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public int GetVncIDForUser(IfyContext context, int userid, string type) {
            var sql = string.Format("SELECT vnc_id FROM vnc_usr WHERE vnc_type='{1}' AND id_usr={0};", userid, type);
            return context.GetQueryIntegerValue(sql);
        }

        /// <summary>
        /// Save VNC Connection ID for user on DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userid"></param>
        /// <param name="id"></param>
        /// <param name="type"></param>
        public void SetVncIDForUser(IfyContext context, int userid, int id, string type) {
            var sql = string.Format("INSERT INTO vnc_usr (vnc_id,vnc_type,id_usr) VALUES ('{0}','{2}',{1});", id, userid, type);
            context.Execute(sql);
        }

        /// <summary>
        /// Delete VNC Connection ID for user on DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userid"></param>
        /// <param name="type"></param>
        public void DeleteVncIDForUser(IfyContext context, int userid, string type) {
            var sql = string.Format("DELETE FROM vnc_usr WHERE vnc_type='{1}' AND id_usr={0};", userid, type);
            context.Execute(sql);
        }
    }

    public class K8sPodStatus {
        public static readonly string Running = "Running";
        public static readonly string Pending = "Pending";
    }

}
