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

    [Route ("/community/{id}/activity/search", "GET", Summary = "GET the activities of the community", Notes = "")]
    public class CommunitySearchActivitiesRequestTep : IReturn<HttpResult>
    {
        [ApiMember (Name = "id", Description = "Id of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }
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

        public object Get (CommunitySearchActivitiesRequestTep request) {
            var context = TepWebContext.GetWebContext (PagePrivileges.UserView);
            context.AccessLevel = EntityAccessLevel.Administrator;
            context.Open ();
            context.LogInfo (this, string.Format ("/activity/search GET"));

            List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable> ();

            EntityList<Activity> activities = new EntityList<Activity> (context);
            activities.Load ();

            List<Activity> tmplist = new List<Activity> ();
            tmplist = activities.GetItemsAsList ();
            tmplist.Sort ();
            tmplist.Reverse ();

            activities = new EntityList<Activity> (context);
            activities.Identifier = "activity";
            foreach (Activity item in tmplist) {
                if ((item.Privilege != null && item.Privilege.EntityType != null && item.Privilege.EntityType.Id != EntityType.GetEntityType (typeof (UserTep)).Id && !item.Privilege.Operation.Equals ("l")))
                    activities.Include (item);
            }
            osentities.Add (activities);

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;
            var ose = MasterCatalogue.OpenSearchEngine;

            var multiOSE = new MultiGenericOpenSearchable (osentities, ose);

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest (httpRequest, ose);
            var osr = ose.Query (multiOSE, httpRequest.QueryString, responseType);

            multiOSE.Identifier = "activity";

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks (multiOSE, osr);

            context.Close ();
            return new HttpResult (osr.SerializeToString (), osr.ContentType);
        }
    }
}