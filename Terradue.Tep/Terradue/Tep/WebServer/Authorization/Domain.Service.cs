using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using Terradue.OpenNebula;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class DomainServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(DomainGetRequest request) {
            WebDomain result;

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/domain/{{Id}} GET Id='{0}'", request.Id));
                Domain domain = Domain.FromId(context, request.Id);
                result = new WebDomain(domain);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(DomainsGetRequest request) {
            List<WebDomain> result = new List<WebDomain>();

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/domain GET"));
                EntityList<Domain> domains = new EntityList<Domain>(context);
                domains.Load();
                foreach (Domain g in domains) {
                    if (!request.all && !g.Identifier.StartsWith (context.GetConfigValue("DomainThematicPrefix"))) continue;
                    result.Add (new WebDomain (g));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Put the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Put(DomainUpdateRequest request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebDomain result;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/domain PUT Id='{0}'", request.Id));
                Domain domain = (request.Id == 0 ? null : Domain.FromId(context, request.Id));
                domain = request.ToEntity(context, domain);
                domain.Store();
                context.LogDebug(this,string.Format("Domain {0} updated by user {1}", domain.Identifier, User.FromId(context, context.UserId).Username));
                result = new WebDomain(domain);
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(DomainCreateRequest request)
        {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebDomain result;
            try{
                context.Open();
                Domain domain = (request.Id == 0 ? null : Domain.FromId(context, request.Id));
				domain = request.ToEntity(context, domain);
                domain.Store();
                result = new WebDomain(domain);
                context.LogInfo(this,string.Format("/domain POST Id='{0}'", request.Id));
                context.LogDebug(this,string.Format("Domain {0} created by user {1}", domain.Identifier, User.FromId(context, context.UserId).Username));
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Delete the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Delete(DomainDeleteRequest request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/domain/{{Id}} DELETE Id='{0}'", request.Id));
                Domain domain = Domain.FromId(context, request.Id);
                if (context.UserLevel == UserLevel.Administrator) domain.Delete();
                else throw new UnauthorizedAccessException(CustomErrorMessages.ADMINISTRATOR_ONLY_ACTION);
                context.LogDebug(this,string.Format("Domain {0} deleted by user {1}", domain.Identifier, User.FromId(context, context.UserId).Username));
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

