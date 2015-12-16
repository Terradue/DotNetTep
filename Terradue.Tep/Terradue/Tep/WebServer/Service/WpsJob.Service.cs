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
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

     [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class WpsJobServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object Get(WpsJobsGetRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            List<WebWpsJobTep> result = new List<WebWpsJobTep>();
            try {
                context.Open();

                EntityList<WpsJob> services = new EntityList<WpsJob>(context);
                services.OwnedItemsOnly = true;
                services.Load();

                foreach (WpsJob job in services) {
                    result.Add(new WebWpsJobTep(job));
                }

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }

            return result;
        }

        public object Get(WpsJobSearchRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            object result;
            context.Open();

            EntityList<WpsJob> tmp = new EntityList<WpsJob>(context);
            tmp.Load();

            List<WpsJob> jobs = tmp.GetItemsAsList();
            jobs.Sort();
            jobs.Reverse();

            EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
            foreach (WpsJob job in jobs) wpsjobs.Include(job);

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            string format;
            if (Request.QueryString["format"] == null)
                format = "atom";
            else
                format = Request.QueryString["format"];

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query(wpsjobs, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(wpsjobs, osr);
//            OpenSearchFactory.ReplaceSelfLinks(wpsjobs, httpRequest.QueryString, osr.Result, EntrySelfLinkTemplate);

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }

        public object Get(WpsJobDescriptionRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
                wpsjobs.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = wpsjobs.GetOpenSearchDescription();

                context.Close();

                return new HttpResult(osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.Close();
                throw e;
            }
        }

        public object Post(WpsJobCreateRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            HttpResult result = null;
            context.Open();

            WpsJob job = request.ToEntity(context, new WpsJob(context));
            try{
                job.Store();
                log.Info(string.Format("WpsJob '{0}' created",job.Name));
            }catch(DuplicateEntityIdentifierException e){
                job = WpsJob.FromIdentifier(context, request.Identifier);
                job.Name = request.Name;
                job.Store();
                log.Info(string.Format("WpsJob '{0}' created",job.Name));
            }catch(Exception e){
                throw e;
            }

            EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
            wpsjobs.OwnedItemsOnly = true;
            wpsjobs.Load();

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            string format;
            if (Request.QueryString["format"] == null)
                format = "atom";
            else
                format = Request.QueryString["format"];
               
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("id", job.Identifier);

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(HttpContext.Current.Request, ose);
            IOpenSearchResultCollection osr = ose.Query(wpsjobs, nvc, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(wpsjobs, osr);

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }

        public object Put(WpsJobUpdateRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            HttpResult result = null;
            context.Open();

            WpsJob job = WpsJob.FromIdentifier(context, request.Identifier);
            try{
                job.Name = request.Name;
                job.Store();
                log.Info(string.Format("WpsJob '{0}' updated",job.Name));
            }catch(Exception e){
                throw e;
            }

            EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
            wpsjobs.OwnedItemsOnly = true;
            wpsjobs.Load();

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            string format;
            if (Request.QueryString["format"] == null)
                format = "atom";
            else
                format = Request.QueryString["format"];

            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("id", job.Identifier);

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(HttpContext.Current.Request, ose);
            IOpenSearchResultCollection osr = ose.Query(wpsjobs, nvc, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(wpsjobs, osr);

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }

//        public object Delete(DeleteWPSJob request) {
//            IfyWebContext context = TepQWWebContext.GetWebContext(PagePrivileges.UserView);
//            bool result = false;
//            try {
//                context.Open();
//
//                WpsJob job = null;
//                job = WpsJob.FromId(context, request.id);
//                job.Delete();
//                result = true;
//
//                context.Close();
//            } catch (Exception e) {
//                context.Close();
//                throw e;
//            }
//            return new WebResponseBool(result);
//        }

        public object Delete(WpsJobDeleteRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            bool result = false;
            try {
                context.Open();

                WpsJob job = null;
                job = WpsJob.FromIdentifier(context, request.id);
                job.Delete();
                result = true;

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(result);
        }

        public static string EntrySelfLinkTemplate(IOpenSearchResultItem item, OpenSearchDescription osd, string mimeType) {
            if (item == null)
                return null;

            string identifier = item.Identifier;

            NameValueCollection nvc = new NameValueCollection();

            nvc.Set("id", string.Format("{0}", item.Identifier));

            UriBuilder template = new UriBuilder(OpenSearchFactory.GetOpenSearchUrlByType(osd, mimeType).Template);
            string[] queryString = Array.ConvertAll(nvc.AllKeys, key => string.Format("{0}={1}", key, nvc[key]));
            template.Query = string.Join("&", queryString);
            return template.ToString();
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(WpsJobGetGroupsRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();

                WpsJob job = WpsJob.FromIdentifier(context, request.JobId);

                List<int> ids = job.GetGroupsWithPrivileges();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    result.Add(new WebGroup(grp));
                }

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(WpsJobAddGroupRequestTep request) {

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                WpsJob wps = WpsJob.FromIdentifier(context, request.JobId);

                List<int> ids = wps.GetGroupsWithPrivileges();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    if(grp.Id == request.Id) return new WebResponseBool(false);
                }

                wps.StorePrivilegesForGroups(new int[]{request.Id});

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Put the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Put(WpsJobUpdateGroupsRequestTep request) {

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                WpsJob wps = WpsJob.FromIdentifier(context, request.JobId);

                string sql = String.Format("DELETE FROM wpsjob_priv WHERE id_wpsjob={0} AND id_grp IS NOT NULL;",wps.Id);
                context.Execute(sql);

                wps.StorePrivilegesForGroups(request.ToArray());

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Delete(WpsJobDeleteGroupRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();

                WpsJob job = WpsJob.FromIdentifier(context, request.JobId);

                //TODO: replace once http://project.terradue.com/issues/13954 is resolved
                string sql = String.Format("DELETE FROM wpjob_priv WHERE id_wpsjob={0} AND id_grp={1};",request.JobId, job.Id);
                context.Execute(sql);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }
    }
}

