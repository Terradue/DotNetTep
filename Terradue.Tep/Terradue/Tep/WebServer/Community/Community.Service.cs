using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
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
                Role role = Role.FromIdentifier (context, string.IsNullOrEmpty (request.Role) ? RoleTep.MEMBER : request.Role);
                context.LogInfo (this, string.Format ("/community/user POST Identifier='{0}', Username='{1}', Role='{2}'", request.Identifier, user.Username, role.Identifier));

                //we use administrator access level to be able to load the community
                context.AccessLevel = EntityAccessLevel.Administrator;
                ThematicCommunity domain = ThematicCommunity.FromIdentifier (context, request.Identifier);

                if (string.IsNullOrEmpty(request.Username)) {
                    //case user auto Join 
                    domain.JoinCurrentUser();
                } else { 
                    //case owner add user with role
                    domain.SetUserAsTemporaryMember(user, role.Id);
                }

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

                domain.SetUserRole(user, role);

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

                if (!domain.IsUserOwner(context.UserId)) throw new UnauthorizedAccessException ("Action only allowed to manager of the domain");

                domain = request.ToEntity(context, domain);
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

                domain.RemoveUser(user);

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
            context.Open ();
            context.LogInfo (this, string.Format ("/community/search GET"));

            EntityList<ThematicCommunity> domains = new EntityList<ThematicCommunity> (context);
            domains.SetFilter("Kind", (int)DomainKind.Public + "," + (int)DomainKind.Private);

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;
            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
            Type responseType = OpenSearchFactory.ResolveTypeFromRequest (httpRequest, ose);

            // the opensearch cache system uses the query parameters
            // we add to the parameters the filters added to the load in order to avoir wrong cache
            // we use 't2-' in order to not interfer with possibly used query parameters
            var qs = new NameValueCollection(Request.QueryString);
            foreach (var filter in domains.FilterValues) qs.Add("t2-" + filter.Key.FieldName, filter.Value);

            //IOpenSearchResultCollection result;

            //httpRequest.QueryString.Set("status", "joined");
            IOpenSearchResultCollection osr = ose.Query (domains, httpRequest.QueryString, responseType);
            //result = osr;

            //httpRequest.QueryString.Set("status", "unjoined");
            //osr = ose.Query(domains, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks (domains, osr);

            context.Close ();
            return new HttpResult (osr.SerializeToString (), osr.ContentType);
        }

        public object Get(CommunityDescriptionRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/community/description GET"));

                EntityList<ThematicCommunity> domains = new EntityList<ThematicCommunity>(context);
                domains.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = domains.GetOpenSearchDescription();

                context.Close();

                return new HttpResult(osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
        }
    }
}