using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services{
    
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
                if (string.IsNullOrEmpty (request.Identifier)) throw new Exception ("Invalid request - missing community identifier");

                User user = string.IsNullOrEmpty(request.Username) ? User.FromId (context, context.UserId) : User.FromUsername(context, request.Username);
                Role role = Role.FromIdentifier (context, string.IsNullOrEmpty (request.Role) ? ThematicCommunity.MEMBER : request.Role);
                context.LogInfo (this, string.Format ("/community/user POST Identifier='{0}', Username='{1}', Role='{2}'", request.Identifier, user.Username, role.Identifier));

                ThematicCommunity domain = ThematicCommunity.FromIdentifier (context, request.Identifier);

                if (string.IsNullOrEmpty (request.Username)) { 
                    if (!domain.IsUserManager (context.UserId)) throw new UnauthorizedAccessException ("Action only allowed to manager of the domain");
                }

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
        /// Put the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Put (CommunityUpdateUserRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                if (string.IsNullOrEmpty (request.Identifier)) throw new Exception ("Invalid request - missing community identifier");
                if (string.IsNullOrEmpty (request.Username)) throw new Exception ("Invalid request - missing username");
                if (string.IsNullOrEmpty (request.Role)) throw new Exception ("Invalid request - missing role");

                User user = User.FromUsername (context, request.Username);
                Role role = Role.FromIdentifier (context, request.Role);
                context.LogInfo (this, string.Format ("/community/user PUT Identifier='{0}', Username='{1}', Role='{2}'", request.Identifier, user.Username, role.Identifier));

                ThematicCommunity domain = ThematicCommunity.FromIdentifier (context, request.Identifier);

                if (!domain.IsUserManager (context.UserId)) throw new UnauthorizedAccessException ("Action only allowed to manager of the domain");

                //delete previous role(s)
                var roles = Role.GetUserRolesForDomain (context, user.Id, domain.Id);
                foreach (var r in roles) r.RevokeFromUser (user, domain);

                //add new role
                role.GrantToUser (user, domain);

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool (true);
        }

        public object Put (CommunityUpdateRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                if (string.IsNullOrEmpty (request.Identifier)) throw new Exception ("Invalid request - missing community identifier");

                context.LogInfo (this, string.Format ("/community PUT Identifier='{0}'", request.Identifier));

                ThematicCommunity domain = ThematicCommunity.FromIdentifier (context, request.Identifier);

                if (!domain.IsUserManager(context.UserId)) throw new UnauthorizedAccessException ("Action only allowed to manager of the domain");

                domain.Description = request.Description;
                domain.Name = request.Name;
                domain.Store ();

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool (true);
        }

        public object Delete (CommunityRemoveUserRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.DeveloperView);

            try {
                context.Open ();
                if (string.IsNullOrEmpty (request.Identifier)) throw new Exception ("Invalid request - missing community identifier");

                User user = string.IsNullOrEmpty (request.Username) ? User.FromId (context, context.UserId) : User.FromUsername (context, request.Username);
                context.LogInfo (this, string.Format ("/community/user DELETE Identifier='{0}', Username='{1}'", request.Identifier, request.Username));

                ThematicCommunity domain = ThematicCommunity.FromIdentifier (context, request.Identifier);

                if (!domain.IsUserManager (context.UserId)) throw new UnauthorizedAccessException ("Action only allowed to manager of the domain");

                if (string.IsNullOrEmpty (request.Username)) {
                    if (!domain.IsUserManager (context.UserId)) throw new UnauthorizedAccessException ("Action only allowed to manager of the domain");
                }

                //delete previous role(s)
                var uroles = Role.GetUserRolesForDomain (context, user.Id, domain.Id);
                foreach (var r in uroles) r.RevokeFromUser (user, domain);

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

        public object Get (CommunitySearchRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            object result;
            context.Open ();
            context.LogInfo (this, string.Format ("/community/search GET"));

            EntityList<ThematicCommunity> domains = new EntityList<ThematicCommunity> (context);

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            string format;
            if (Request.QueryString ["format"] == null)
                format = "atom";
            else
                format = Request.QueryString ["format"];

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest (httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query (domains, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks (domains, osr);

            context.Close ();
            return new HttpResult (osr.SerializeToString (), osr.ContentType);
        }
    }
}