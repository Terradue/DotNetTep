using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services{

    [Route ("/community/{id}/user", "POST", Summary = "POST the current user into the community", Notes = "")]
    public class CommunityAddUserRequestTep : IReturn<WebResponseBool> {
        [ApiMember (Name = "id", Description = "Id of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route ("/community/{id}/user", "DELETE", Summary = "DELETE the current user from the community", Notes = "")]
    public class CommunityRemoveUserRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "id", Description = "Id of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route ("/job/wps/{id}/community/{cid}", "PUT", Summary = "PUT the wpsjob to the community", Notes = "")]
    public class CommunityAddWpsJobRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "id", Description = "Id of the wps job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember (Name = "cid", Description = "Id of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int CId { get; set; }
    }

    [Route ("/data/package/{id}/community/{cid}", "PUT", Summary = "PUT the data package to the community", Notes = "")]
    public class CommunityAddDataPackageRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "id", Description = "Id of the data package", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember (Name = "cid", Description = "Id of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int CId { get; set; }
    }

    [Api ("Tep Terradue webserver")]
    [Restrict (EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
             EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class CommunityServiceTep : ServiceStack.ServiceInterface.Service
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post (CommunityAddUserRequestTep request) {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/community/{{Id}}/user POST Id='{0}'", request.Id));

                Domain domain = Domain.FromId (context, request.Id);
                Role role = Role.FromIdentifier (context, "member");
                User user = User.FromId (context, context.UserId);
                role.GrantToUser (user, domain);

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool(true);
        }

        /// <summary>
        /// Delete the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Delete (CommunityRemoveUserRequestTep request) {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/community/{{Id}}/user DELETE Id='{0}'", request.Id));

                Domain domain = Domain.FromId (context, request.Id);
                User user = User.FromId (context, context.UserId);
                var roles = Role.GetUserRolesForDomain (context, user.Id, domain.Id);
                foreach (var role in roles) role.RevokeFromUser (user, domain);

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool (true);
        }

        public object Put (CommunityAddWpsJobRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/job/wps/{{Id}}/community/{{cId}} PUT Id='{0}', CId='{1}", request.Id, request.CId));

                var wpsjob = WpsJob.FromId (context, request.Id);
                wpsjob.DomainId = request.CId;
                wpsjob.Store ();

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool (true);
        }

        public object Put (CommunityAddDataPackageRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/data/package/{{Id}}/community/{{cId}} PUT Id='{0}', CId='{1}", request.Id, request.CId));

                var dp = DataPackage.FromId (context, request.Id);
                dp.DomainId = request.CId;
                dp.Store ();

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool (true);
        }
    }
}