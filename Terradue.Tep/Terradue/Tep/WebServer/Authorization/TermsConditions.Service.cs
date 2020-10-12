using System;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

    [Route("/termsconditions/user/current", "GET", Summary = "GET terms and conditions", Notes = "")]
    public class GetTermsConditionsForCurrentUserRequestTep : IReturn<WebResponseBool> {
        [ApiMember(Name = "identifier", Description = "t&C identifier", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string identifier { get; set; }
    }

    [Route("/termsconditions/user/current", "POST", Summary = "POST terms and conditions", Notes = "")]
    public class PostTermsConditionsForCurrentUserRequestTep : IReturn<WebResponseBool> {
        [ApiMember(Name = "identifier", Description = "t&c identifier", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string identifier { get; set; }
    }

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
                  EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class TermsConditionsServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(GetTermsConditionsForCurrentUserRequestTep request) {
            WebResponseBool response;

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/termsconditions/user/current GET"));

                var check = TermsConditionsHelper.CheckTermsConditionsForCurrentUser(context, request.identifier);
                response = new WebResponseBool(check);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return response;
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(PostTermsConditionsForCurrentUserRequestTep request) {

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/termsconditions/user/current POST"));

                TermsConditionsHelper.ApproveTermsConditionsForCurrentUser(context, request.identifier);                

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }
    }
}
