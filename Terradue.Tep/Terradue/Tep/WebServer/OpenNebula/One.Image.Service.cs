using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using Terradue.Cloud;
using Terradue.OpenNebula;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class OneImageServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(OneGetImageRequestTep request) {
            string result = null;

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/one/{{providerId}}/img/{{imageId}} GET providerId='{0}',imageId='{1}'", request.ProviderId, request.ImageId));
                int provId = (request.ProviderId != 0 ? request.ProviderId : context.GetConfigIntegerValue("One-default-provider"));
                OneCloudProvider oneCloud = (OneCloudProvider)CloudProvider.FromId(context, provId);
                IMAGE oneuser = oneCloud.XmlRpc.ImageGetInfo(request.ImageId);
                result = oneuser.NAME;
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

    }

    [Route("/one/img/{imageId}", "GET", Summary = "GET an image of opennebula", Notes = "Get OpenNebula image")]
    [Route("/one/{providerId}/img/{imageId}", "GET", Summary = "GET an image of opennebula", Notes = "Get OpenNebula image")]
    public class OneGetImageRequestTep : IReturn<List<string>> {
        [ApiMember(Name = "providerId", Description = "Provider id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int ProviderId { get; set; }
        [ApiMember(Name = "imageId", Description = "Image id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int ImageId { get; set; }
    }

}

