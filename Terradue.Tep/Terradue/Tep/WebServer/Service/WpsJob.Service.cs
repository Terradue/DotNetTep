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
using System.Linq;
using System.Net;
using OpenGis.Wps;
using Terradue.Tep.OpenSearch;

namespace Terradue.Tep.WebServer.Services {

     [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class WpsJobServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(WpsJobsGetRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            List<WebWpsJobTep> result = new List<WebWpsJobTep>();
            try {
                context.Open();
                context.LogInfo(this,string.Format("/job/wps GET"));

                EntityList<WpsJob> services = new EntityList<WpsJob>(context);
                if(context.UserLevel != UserLevel.Administrator) services.ItemVisibility = EntityItemVisibility.OwnedOnly;
                services.Load();

                foreach (WpsJob job in services) {
                    result.Add(new WebWpsJobTep(job, context));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }

            return result;
        }

        public object Get(WpsJobGetOneRequestTep request){
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebWpsJobTep result = new WebWpsJobTep();
            try {
                context.Open();
                context.ConsoleDebug = true;
                context.LogInfo(this,string.Format("/job/wps/{{Id}} GET Id='{0}'", request.Id));

                WpsJob job = WpsJob.FromId(context, request.Id);
                result = new WebWpsJobTep(job, context);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }

            return result;
        }

        public object Get(WpsJobSearchRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();
            context.LogInfo(this,string.Format("/job/wps/search GET"));

            EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
            wpsjobs.AccessLevel = EntityAccessLevel.Privilege;//for admin not to get all items when visibility is set
            wpsjobs.AddSort("Id", SortDirection.Descending);

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
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/job/wps/description GET"));

                EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
                wpsjobs.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = wpsjobs.GetOpenSearchDescription();

                context.Close();

                return new HttpResult(osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
        }

        public object Get (WpsJobProductSearchRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            HttpResult result = null;
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/job/wps/{0}/products/search GET", request.JobId));

                WpsJob wpsjob = null;

				try {
					wpsjob = WpsJob.FromIdentifier(context, request.JobId);
				} catch (Exception e) {
					if (request.Key != null){//or if public
						context.AccessLevel = EntityAccessLevel.Administrator;
						wpsjob = WpsJob.FromIdentifier(context, request.JobId);
						if (request.Key != null && !request.Key.Equals(wpsjob.AccessKey))
							throw new UnauthorizedAccessException(CustomErrorMessages.WRONG_ACCESSKEY);
					}
				}

                OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
                HttpRequest httpRequest = HttpContext.Current.Request;
                var type = OpenSearchFactory.ResolveTypeFromRequest (httpRequest, ose);
                var nvc = httpRequest.QueryString;

                WpsJobProductOpenSearchable wpsjobProductOs = new WpsJobProductOpenSearchable (wpsjob,context);

                //var nvc = wpsjobUrl.GetParameters ();
                var res = ose.Query (wpsjobProductOs, nvc, type);
                OpenSearchFactory.ReplaceSelfLinks(wpsjobProductOs, httpRequest.QueryString, res, EntrySelfLinkTemplate);
                OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(wpsjobProductOs, res);
                result = new HttpResult (res.SerializeToString (), res.ContentType);

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }
            return result;
        }

        string EntrySelfLinkTemplate(IOpenSearchResultItem item, OpenSearchDescription osd, string mimeType) {
            string identifier = item.Identifier;
            return EntrySelfLinkTemplate(identifier, osd, mimeType);
        }

        string EntrySelfLinkTemplate(string identifier, OpenSearchDescription osd, string mimeType) {
            if (identifier == null)
                return null;
            NameValueCollection nvc = OpenSearchFactory.GetOpenSearchParameters(OpenSearchFactory.GetOpenSearchUrlByType(osd, mimeType));
            nvc.Set("uid", string.Format("{0}", identifier));
            nvc.AllKeys.FirstOrDefault(k => {
                if (nvc[k] == "{geo:uid?}")
                    nvc[k] = identifier;
                var matchParamDef = System.Text.RegularExpressions.Regex.Match(nvc[k], @"^{([^?]+)\??}$");
                if (matchParamDef.Success)
                    nvc.Remove(k);
                return false;
            });
            UriBuilder template = new UriBuilder(OpenSearchFactory.GetOpenSearchUrlByType(osd, mimeType).Template);
            string[] queryString = Array.ConvertAll(nvc.AllKeys, key => string.Format("{0}={1}", key, nvc[key]));
            template.Query = string.Join("&", queryString);
            return template.ToString();
        }

        //public static string EntrySelfLinkTemplate(IOpenSearchResultItem item, OpenSearchDescription osd, string mimeType) {
        //    if (item == null)
        //        return null;

        //    string identifier = item.Identifier;

        //    NameValueCollection nvc = new NameValueCollection();

        //    nvc.Set("id", string.Format("{0}", item.Identifier));

        //    UriBuilder template = new UriBuilder(OpenSearchFactory.GetOpenSearchUrlByType(osd, mimeType).Template);
        //    string[] queryString = Array.ConvertAll(nvc.AllKeys, key => string.Format("{0}={1}", key, nvc[key]));
        //    template.Query = string.Join("&", queryString);
        //    return template.ToString();
        //}

        public object Get (WpsJobProductDescriptionRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            HttpResult result = null;
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/job/wps/{0}/products/description GET", request.JobId));

                WpsJob wpsjob = WpsJob.FromIdentifier(context, request.JobId);

                OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
                HttpRequest httpRequest = HttpContext.Current.Request;

                WpsJobProductOpenSearchable wpsjobProductOs = new WpsJobProductOpenSearchable(wpsjob, context);
                OpenSearchDescription osd = wpsjobProductOs.GetProxyOpenSearchDescription ();
                result = new HttpResult (osd, "application/opensearchdescription+xml");

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }
            return result;
        }

        public object Post(WpsJobCreateRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            context.Open();

            WpsJob job = request.ToEntity(context, new WpsJob(context));
            try{
                job.Store();
            }catch(DuplicateEntityIdentifierException){
                job = WpsJob.FromIdentifier(context, request.Identifier);
                job.Name = request.Name;
                job.Store();
            }catch(Exception e){
                throw e;
            }

            context.LogInfo(this,string.Format("/job/wps POST Id='{0}'",job.Id));
            context.LogDebug(this,string.Format("WpsJob '{0}' created",job.Name));

            EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
            wpsjobs.ItemVisibility = EntityItemVisibility.OwnedOnly;
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
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            context.Open();
            context.LogInfo(this,string.Format("/job/wps PUT Id='{0}'",request.Id));

            WpsJob job = WpsJob.FromIdentifier(context, request.Identifier);
            try{
                job.Name = request.Name;
                job.Store();
                context.LogDebug(this,string.Format("WpsJob '{0}' updated",job.Name));
            }catch(Exception e){
                throw e;
            }

            EntityList<WpsJob> wpsjobs = new EntityList<WpsJob>(context);
            wpsjobs.ItemVisibility = EntityItemVisibility.OwnedOnly;
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

        public object Delete(WpsJobDeleteRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            bool result = false;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/job/wps/{{Id}} DELETE Id='{0}'",request.id));

                WpsJob job = null;
                job = WpsJob.FromIdentifier(context, request.id);
                job.Delete();
                result = true;

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(result);
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(WpsJobGetGroupsRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/job/wps/{{jobId}}/group GET jobId='{0}'",request.JobId));

                WpsJob job = WpsJob.FromIdentifier(context, request.JobId);

                List<int> ids = job.GetAuthorizedGroupIds().ToList();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    result.Add(new WebGroup(grp));
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
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(WpsJobAddGroupRequestTep request) {

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/job/wps/{{jobId}}/group POST jobId='{0}',Id='{1}'",request.JobId, request.Id));
                WpsJob wps = WpsJob.FromIdentifier(context, request.JobId);

                List<int> ids = wps.GetAuthorizedGroupIds().ToList();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    if(grp.Id == request.Id) return new WebResponseBool(false);
                }
                wps.GrantPermissionsToGroups(new int[]{request.Id});

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
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

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/job/wps/{{jobId}}/group PUT jobId='{0}',Id='{1}'",request.JobId, request.ToArray() != null ? string.Join(",",request.ToArray()) : "null"));
                WpsJob wps = WpsJob.FromIdentifier(context, request.JobId);

                string sql = String.Format("DELETE FROM wpsjob_perm WHERE id_wpsjob={0} AND id_grp IS NOT NULL;",wps.Id);
                context.Execute(sql);

                wps.GrantPermissionsToGroups(request.ToArray());

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
        /// <param name="request">Request.</param>
        public object Delete(WpsJobDeleteGroupRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/job/wps/{{jobId}}/group/{{Id}} DELETE jobId='{0}',Id='{1}'",request.JobId, request.Id));

                WpsJob job = WpsJob.FromIdentifier(context, request.JobId);

                //TODO: replace once http://project.terradue.com/issues/13954 is resolved
                string sql = String.Format("DELETE FROM wpjob_perm WHERE id_wpsjob={0} AND id_grp={1};",request.JobId, job.Id);
                context.Execute(sql);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Put (WpsJobCopyRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.AdminOnly);
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/job/wps/copy PUT Id='{0}'", request.Id));

                WpsJob job = WpsJob.FromId (context, request.Id);

                WpsJob newjob = new WpsJob (context);
                newjob.OwnerId = context.UserId;
                newjob.UserId = context.UserId;
                newjob.Identifier = Guid.NewGuid().ToString();
                newjob.StatusLocation = job.StatusLocation;
                newjob.Parameters = job.Parameters;
                newjob.CreatedTime = job.CreatedTime;
                newjob.Name = job.Name;
                newjob.ProcessId = job.ProcessId;
                newjob.RemoteIdentifier = job.RemoteIdentifier;
                newjob.WpsId = job.WpsId;
                newjob.Store ();

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

