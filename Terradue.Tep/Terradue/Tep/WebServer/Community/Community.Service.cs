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
            var context = TepWebContext.GetWebContext (PagePrivileges.UserView);

            try {
                context.Open ();
                if (string.IsNullOrEmpty (request.Identifier)) throw new Exception ("Invalid request - missing community identifier");

                User user = string.IsNullOrEmpty(request.Username) ? User.FromId (context, context.UserId) : User.FromUsername(context, request.Username);

                //we use administrator access level to be able to load the community
                context.AccessLevel = EntityAccessLevel.Administrator;
                ThematicCommunity domain = ThematicCommunity.FromIdentifier (context, request.Identifier);

                Role role = Role.FromIdentifier(context, string.IsNullOrEmpty(request.Role) ? domain.DefaultRoleName : request.Role);
                context.LogInfo(this, string.Format("/community/user POST Identifier='{0}', Username='{1}', Role='{2}'", request.Identifier, user.Username, role.Identifier));

                if (string.IsNullOrEmpty(request.Username)) {
                    //case user auto Join 
                    domain.JoinCurrentUser();
                } else {
                    //case owner add user with role
                    domain.SetUserRole(user, role);
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
            var context = TepWebContext.GetWebContext (PagePrivileges.UserView);

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
            var context = TepWebContext.GetWebContext (PagePrivileges.UserView);

            try {
                context.Open ();
                if (string.IsNullOrEmpty (request.Identifier)) throw new Exception ("Invalid request - missing community identifier");

                context.LogInfo (this, string.Format ("/community PUT Identifier='{0}'", request.Identifier));

                ThematicCommunity domain = ThematicCommunity.FromIdentifier (context, request.Identifier);

                if (!domain.CanUserManage(context.UserId)) throw new UnauthorizedAccessException ("Action only allowed to manager of the domain");

                domain = request.ToEntity(context, domain);
                domain.Store ();

                //store appslinks
                var app = domain.GetThematicApplication();
                //delete old links
                app.LoadItems();
                foreach (var resource in app.Items) {
                    resource.Delete();
                }
                app.LoadItems();
                //add new links
                foreach (var link in request.Apps) {                    
                    var res = new RemoteResource(context);
                    res.Location = link;
                    app.AddResourceItem(res);
                }

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }

            return new WebResponseBool (true);
        }

        public object Post(CommunityCreateRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);

            try {
                context.Open();
                if (string.IsNullOrEmpty(request.Identifier)) throw new Exception("Invalid request - missing community identifier");

                context.LogInfo(this, string.Format("/community POST Identifier='{0}'", request.Identifier));

                ThematicCommunity domain = new ThematicCommunity(context);
                domain = request.ToEntity(context, domain);
                domain.Store();

                var manager = Role.FromIdentifier(context, RoleTep.MANAGER);
                User usr = User.FromId(context, context.UserId);
                manager.GrantToUser(usr, domain);

                //store appslinks
                var app = domain.GetThematicApplication();
                //add new links
                foreach (var link in request.Apps) {                    
                    var res = new RemoteResource(context);
                    res.Location = link;
                    app.AddResourceItem(res);
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }

            return new WebResponseBool(true);
        }

        public object Delete (CommunityRemoveUserRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.UserView);

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

            CommunityCollection domains = new CommunityCollection(context);

            if (!string.IsNullOrEmpty(request.ApiKey)) {
				UserTep user = UserTep.FromApiKey(context, request.ApiKey);
				domains.UserId = user.Id;
				context.AccessLevel = EntityAccessLevel.Privilege;
			}

            if (context.UserId == 0 && !string.IsNullOrEmpty(request.ApiKey)) domains.SetFilter("Kind", (int)DomainKind.Public + "");
            else {
                domains.SetFilter("Kind", (int)DomainKind.Public + "," + (int)DomainKind.Private);
                domains.AddSort("Kind", SortDirection.Ascending);
            }

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;
            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
            Type responseType = OpenSearchFactory.ResolveTypeFromRequest (httpRequest, ose);

            // the opensearch cache system uses the query parameters
            // we add to the parameters the filters added to the load in order to avoir wrong cache
            // we use 't2-' in order to not interfer with possibly used query parameters
            var qs = new NameValueCollection(Request.QueryString);
            foreach (var filter in domains.FilterValues) qs.Add("t2-" + filter.Key.FieldName, filter.Value.ToString());

            IOpenSearchResultCollection osr = ose.Query (domains, httpRequest.QueryString, responseType);

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

        /// <summary>
        /// Delete the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Delete(CommunityDeleteRequest request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/community/{{Identifier}} DELETE Identifier='{0}'", request.Identifier));
                ThematicCommunity domain = ThematicCommunity.FromIdentifier(context, request.Identifier);
                if (domain.CanUserManage(context.UserId)) domain.Delete();
                else throw new UnauthorizedAccessException(CustomErrorMessages.ADMINISTRATOR_ONLY_ACTION);
                context.LogDebug(this,string.Format("Community {0} deleted by user {1}", domain.Identifier, User.FromId(context, context.UserId).Username));
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Delete the specified request.
        /// </summary>
        /// <returns>The delete.</returns>
        /// <param name="request">Request.</param>
        public object Delete(CommunityRemoveCollectionRequestTep request) {
			var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			try {
				context.Open();
                context.AccessLevel = EntityAccessLevel.Privilege;
                context.LogInfo(this, string.Format("/community/{{Identifier}}/collection/{{CollIdentifier}} DELETE Identifier='{0}' , CollIdentifier='{1}'", request.Identifier, request.CollIdentifier));
				var domain = ThematicCommunity.FromIdentifier(context, request.Identifier);
				if (!domain.CanUserManageCollection(context.UserId)) throw new UnauthorizedAccessException(CustomErrorMessages.ADMINISTRATOR_ONLY_ACTION);
                var collection = Collection.FromIdentifier(context, request.CollIdentifier);
                var owner = User.FromId(context, collection.UserId);
                collection.Domain = owner.Domain;
                collection.Store();
				context.LogDebug(this, string.Format("Collection removed from Community {0}, put in owner's domain {1}", domain.Identifier, User.FromId(context, context.UserId).Username));
				context.Close();
			} catch (Exception e) {
				context.LogError(this, e.Message);
				context.Close();
				throw e;
			}
			return new WebResponseBool(true);
		}

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <returns>The post.</returns>
        /// <param name="request">Request.</param>
		public object Post(CommunityAddCollectionRequestTep request) {
			var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			try {
				context.Open();
                context.AccessLevel = EntityAccessLevel.Privilege;
				context.LogInfo(this, string.Format("/community/{{Identifier}}/collection/{{CollIdentifier}} POST Identifier='{0}' , CollIdentifier='{1}'", request.Identifier, request.CollIdentifier));
				var domain = ThematicCommunity.FromIdentifier(context, request.Identifier);
				if (!domain.CanUserManageCollection(context.UserId)) throw new UnauthorizedAccessException(CustomErrorMessages.ADMINISTRATOR_ONLY_ACTION);
				var collection = Collection.FromIdentifier(context, request.CollIdentifier);
                collection.AccessLevel = EntityAccessLevel.Privilege;
				collection.Domain = domain;
				collection.Store();
				context.LogDebug(this, string.Format("Collection added to Community {0}", domain.Identifier));
				context.Close();
			} catch (Exception e) {
				context.LogError(this, e.Message);
				context.Close();
				throw e;
			}
			return new WebResponseBool(true);
		}

		/// <summary>
		/// Delete the specified request.
		/// </summary>
		/// <returns>The delete.</returns>
		/// <param name="request">Request.</param>
		public object Delete(CommunityRemoveWpsServiceRequestTep request) {
			var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			try {
				context.Open();
				context.AccessLevel = EntityAccessLevel.Privilege;
				context.LogInfo(this, string.Format("/community/{{Identifier}}/service/wps/{{WpsIdentifier}} DELETE Identifier='{0}' , WpsIdentifier='{1}'", request.Identifier, request.WpsIdentifier));
				var domain = ThematicCommunity.FromIdentifier(context, request.Identifier);
				if (!domain.CanUserManageService(context.UserId)) throw new UnauthorizedAccessException(CustomErrorMessages.ADMINISTRATOR_ONLY_ACTION);
                var wps = WpsProcessOffering.FromIdentifier(context, request.WpsIdentifier);
                var owner = User.FromId(context, wps.OwnerId != 0 ? wps.OwnerId : wps.UserId);
				wps.Domain = owner.Domain;
				wps.Store();
				context.LogDebug(this, string.Format("Wps service removed from Community {0}, put in owner's domain {1}", domain.Identifier, User.FromId(context, context.UserId).Username));
				context.Close();
			} catch (Exception e) {
				context.LogError(this, e.Message);
				context.Close();
				throw e;
			}
			return new WebResponseBool(true);
		}

		/// <summary>
		/// Post the specified request.
		/// </summary>
		/// <returns>The post.</returns>
		/// <param name="request">Request.</param>
		public object Post(CommunityAddWpsServiceRequestTep request) {
			var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			try {
				context.Open();
				context.AccessLevel = EntityAccessLevel.Privilege;
				context.LogInfo(this, string.Format("/community/{{Identifier}}/service/wps/{{WpsIdentifier}} POST Identifier='{0}' , WpsIdentifier='{1}'", request.Identifier, request.WpsIdentifier));
				var domain = ThematicCommunity.FromIdentifier(context, request.Identifier);
				if (!domain.CanUserManageService(context.UserId)) throw new UnauthorizedAccessException(CustomErrorMessages.ADMINISTRATOR_ONLY_ACTION);
				var wps = WpsProcessOffering.FromIdentifier(context, request.WpsIdentifier);
				wps.AccessLevel = EntityAccessLevel.Privilege;
				wps.Domain = domain;
				wps.Store();
				context.LogDebug(this, string.Format("Wps service added to Community {0}", domain.Identifier));
				context.Close();
			} catch (Exception e) {
				context.LogError(this, e.Message);
				context.Close();
				throw e;
			}
			return new WebResponseBool(true);
		}
    }
}