using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep.Controller;
using Terradue.Tep.Controller.Kubernetes.Kubectl;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/user/current/qgis", "GET")]
    public class GetQgisIPRequest : IReturn<WebResponseString> { }

    [Route("/user/current/qgis", "POST")]
    public class CreateQgisRequest : IReturn<WebResponseString> {
        [ApiMember(Name = "appId", Description = "app Id", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string AppId { get; set; }
    }

    [Route("/user/current/qgis", "DELETE")]
    public class DeleteQgisRequest : IReturn<WebResponseBool> {
        [ApiMember(Name = "appId", Description = "app Id", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string AppId { get; set; }
    }

    [Api("Terradue Corporate webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    /// <summary>
    /// OAuth service. Used to log into the system
    /// </summary>
    public class KubernetesService : ServiceStack.ServiceInterface.Service {

        public object Get(GetQgisIPRequest request) {
            string url = "";
            TepWebContext context = new TepWebContext(PagePrivileges.UserView);
            try {
                context.Open();

                context.LogInfo(this, string.Format("/user/current/qgis GET"));

                var user = UserTep.FromId(context, context.UserId);
                url = GetUserVncUrl(context, user, false);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return new WebResponseString(url ?? "");
        }

        public object Post(CreateQgisRequest request) {
            string url = "";
            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();

                context.LogInfo(this, string.Format("/user/current/qgis POST"));

                var user = UserTep.FromId(context, context.UserId);

                url = GetUserVncUrl(context, user, true);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return new WebResponseString(url);
        }

        public object Delete(DeleteQgisRequest request) {
            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();

                context.LogInfo(this, string.Format("/user/current/qgis DELETE"));

                var user = User.FromId(context, context.UserId);

                var k8sFactory = new KubernetesFactory(context);
                var type = "qgis";

                var podID = k8sFactory.GetVncIDForUser(context, user.Id, type);
                try { k8sFactory.DeleteUserEnvironment(user.Username); } catch (Exception e) { context.LogError(this, e.Message); }
                try { k8sFactory.RevokeConnectionPermission(podID, user.Username); } catch (Exception e) { context.LogError(this, e.Message); }
                try { k8sFactory.DeleteGuacamoleConnection(podID); } catch (Exception e) { context.LogError(this, e.Message); }
                try { k8sFactory.DeleteVncIDForUser(context, user.Id, type); } catch (Exception e) { context.LogError(this, e.Message); }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return new WebResponseBool(true);
        }

        private string GetUserVncUrl(IfyContext context, User user, bool createPod = false) {

            var k8sFactory = new KubernetesFactory(context);

            //check if connection already exists and is still valid
            var type = "qgis";
            var id = k8sFactory.GetVncIDForUser(context, user.Id, type);
            if (id != 0) {
                if (k8sFactory.ExistsGuacamoleConnection(id))
                    return k8sFactory.GetGuacamoleVncUrl(id);
                else
                    k8sFactory.DeleteVncIDForUser(context, user.Id, type);
            }

            //check if pod already exists
            var pod = k8sFactory.GetPod(user.Username);

            //if not, we may create it (optional)
            if (pod == null && createPod) {                
                List<KubectlVolumeMount> volumeMounts = GetVolumeMounts(user);
                List<KubectlVolume> volumes = GetVolumes(user);
                pod = k8sFactory.CreateUserQgisEnvironment(user.Username, volumeMounts, volumes);
            }

            //if exists, we save info on guacamole and return the url
            if (pod != null) {

                if (!k8sFactory.IsUserOnGuacamole(user.Username))
                    k8sFactory.CreateUserOnGuacamole(user.Username);

                int podID;
                if (!k8sFactory.IsPodOnGuacamole(pod.Identifier))
                    podID = k8sFactory.CreateGuacamoleVncConnection(user.Username, pod.Identifier, pod.IP);
                else
                    podID = k8sFactory.GetGuacamoleConnectionID(pod.Identifier);

                if (!k8sFactory.ExistsGuacamoleUserConnectionPermission(podID, user.Username))
                    k8sFactory.AddConnectionPermission(podID, user.Username);

                k8sFactory.SetVncIDForUser(context, user.Id, podID, type);

                return k8sFactory.GetGuacamoleVncUrl(podID);
            }

            return null;
        }

        protected List<KubectlVolumeMount> GetVolumeMounts(User user) {
            return new List<KubectlVolumeMount>();
        }

        protected List<KubectlVolume> GetVolumes(User user) {
            return new List<KubectlVolume>();
        }

    }

}
