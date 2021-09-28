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
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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
            wpsjobs.AddSort("Id", SortDirection.Descending);
            wpsjobs.IncludeOwnerFieldsInSearch = true;

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;
            var qs = new NameValueCollection(httpRequest.QueryString);

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            if(qs["visibility"] != null && qs["visibility"] != "all"){
                wpsjobs.AccessLevel = EntityAccessLevel.Privilege;
            }

            if (string.IsNullOrEmpty(qs["id"]) && string.IsNullOrEmpty(qs["uid"]) && string.IsNullOrEmpty(qs["archivestatus"]))
                qs.Set("archivestatus", (int)WpsJobArchiveStatus.NOT_ARCHIVED + "");

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query(wpsjobs, qs, responseType);

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
                context.LogError(this, e.Message, e);
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
                    } else throw e;
				}

                OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;
                HttpRequest httpRequest = HttpContext.Current.Request;
                var type = OpenSearchFactory.ResolveTypeFromRequest (httpRequest, ose);
                var nvc = httpRequest.QueryString;

                if (new Uri(wpsjob.StatusLocation).Host == new Uri(ProductionResultHelper.catalogBaseUrl).Host) {
                    var settings = MasterCatalogue.OpenSearchFactorySettings;
                    OpenSearchableFactorySettings specsettings = (OpenSearchableFactorySettings)settings.Clone();

                    //get credentials from current user
                    if (context.UserId != 0) {
                        var user = UserTep.FromId(context, context.UserId);
                        var apikey = user.GetSessionApiKey();
                        var t2userid = user.TerradueCloudUsername;
                        if (!string.IsNullOrEmpty(apikey)) {
                            specsettings.Credentials = new System.Net.NetworkCredential(t2userid, apikey);
                        }
                    }
                    GenericOpenSearchable urlToShare = new GenericOpenSearchable(new OpenSearchUrl(wpsjob.StatusLocation), specsettings);
                    var res = ose.Query(urlToShare, nvc, type);
                    result = new HttpResult(res.SerializeToString(), res.ContentType);
                } else {

                    WpsJobProductOpenSearchable wpsjobProductOs = new WpsJobProductOpenSearchable(wpsjob, context);

                    //var nvc = wpsjobUrl.GetParameters ();
                    var res = ose.Query(wpsjobProductOs, nvc, type);
                    OpenSearchFactory.ReplaceSelfLinks(wpsjobProductOs, httpRequest.QueryString, res, EntrySelfLinkTemplate);
                    OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(wpsjobProductOs, res);
                    result = new HttpResult(res.SerializeToString(), res.ContentType);
                }

                context.Close ();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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

                OpenSearchDescription osd;

                if (new Uri(wpsjob.StatusLocation).Host == new Uri(ProductionResultHelper.catalogBaseUrl).Host) {
                    var settings = MasterCatalogue.OpenSearchFactorySettings;
                    OpenSearchableFactorySettings specsettings = (OpenSearchableFactorySettings)settings.Clone();

                    //get credentials from current user
                    if (context.UserId != 0) {
                        var user = UserTep.FromId(context, context.UserId);
                        var apikey = user.GetSessionApiKey();
                        var t2userid = user.TerradueCloudUsername;
                        if (!string.IsNullOrEmpty(apikey)) {
                            specsettings.Credentials = new System.Net.NetworkCredential(t2userid, apikey);
                        }
                    }
                    GenericOpenSearchable urlToShare = new GenericOpenSearchable(new OpenSearchUrl(wpsjob.StatusLocation), specsettings);
                    osd = urlToShare.GetOpenSearchDescription();
                    var oldUri = new UriBuilder(osd.DefaultUrl.Template);
                    var newUri = new UriBuilder(context.BaseUrl + "/job/wps/" + wpsjob.Identifier + "/products/search");
                    newUri.Query = oldUri.Query.TrimStart("?".ToCharArray());
                    osd.DefaultUrl.Template = HttpUtility.UrlDecode(newUri.Uri.AbsoluteUri);
                    foreach (var url in osd.Url) {
                        oldUri = new UriBuilder(url.Template);
                        newUri = new UriBuilder(context.BaseUrl + "/job/wps/" + wpsjob.Identifier + "/products/search");
                        newUri.Query = oldUri.Query.TrimStart("?".ToCharArray());
                        url.Template = HttpUtility.UrlDecode(newUri.Uri.AbsoluteUri);
                    }
                } else {
                    WpsJobProductOpenSearchable wpsjobProductOs = new WpsJobProductOpenSearchable(wpsjob, context);    
                    osd = wpsjobProductOs.GetProxyOpenSearchDescription();
                }

                result = new HttpResult (osd, "application/opensearchdescription+xml");

                context.Close ();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                job.AppIdentifier = request.AppIdentifier;
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
                job.LogJobEvent("Job deleted");
                job.Delete();
                result = true;

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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

                var gids = job.GetAuthorizedGroupIds();
                List<int> ids = gids != null ? gids.ToList() : new List<int>();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    result.Add(new WebGroup(grp));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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

                var gids = wps.GetAuthorizedGroupIds();
                List<int> ids = gids != null ? gids.ToList() : new List<int>();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    if(grp.Id == request.Id) return new WebResponseBool(false);
                }
                wps.GrantPermissionsToGroups(new int[]{request.Id});

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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

                WpsJob job = WpsJob.FromIdentifier (context, request.Identifier);
                WpsJob newjob = WpsJob.Copy(job, context);

                context.Close ();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close ();
                throw e;
            }
            return new WebResponseBool (true);
        }

        public object Post (WpsJobSendContactEmailRequestTep request){
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/job/wps/{{identifier}}/contact POST identifier='{0}', subject='{1}', body='{2}'", 
                                                    request.JobId, request.Subject, request.Body));

                WpsJob job = WpsJob.FromIdentifier(context, request.JobId);

                //user must be the owner of the job
                if (context.UserId != job.OwnerId) throw new Exception("Sorry, you must be the owner of the job to contact the service provider for job analysis.");

                if (job.Provider == null) throw new Exception("Unable to find WPS Provider contact");
                var contact = job.ExtractProviderContact(job.Provider.Contact);

                //send email from job's owner to mailto
                context.SendMail(job.Owner.Email, contact, request.Subject, request.Body);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Post(WpsJobSendSupportEmailRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/job/wps/{{identifier}}/support POST identifier='{0}', subject='{1}', body='{2}'",
                                                    request.JobId, request.Subject, request.Body));

                WpsJob job = WpsJob.FromIdentifier(context, request.JobId);

                //user must be the owner of the job
                if (context.UserId != job.OwnerId) throw new Exception("Sorry, you must be the owner of the job to contact the support for job analysis.");

                var supportModes = context.GetConfigValue("wpsjob-support-mode").Split(',');
                if (supportModes.Contains("mail")) {
                    //send email from job's owner to mailto
                    context.SendMail(job.Owner.Email ?? context.GetConfigValue("MailSenderAddress"), context.GetConfigValue("MailSenderAddress"), request.Subject, request.Body);
                }
                if (supportModes.Contains("jira")) {
                    //create JIRA ticket
                    var components = new List<JiraNameProperty>();
                    var configComponents = context.GetConfigValue("jira-helpdesk-components");
                    if (!string.IsNullOrEmpty(configComponents)) {
                        var componentsList = configComponents.Split(',');
                        foreach (var component in componentsList) {
                            components.Add(new JiraNameProperty { name = component });
                        }
                    }
                    var labels = new List<string>();
                    var configLabels = context.GetConfigValue("jira-helpdesk-labels");
                    if (!string.IsNullOrEmpty(configLabels)) {
                        labels = configLabels.Split(',').ToList();
                    }
                    var owner = job.Owner;
                    if (string.IsNullOrEmpty(owner.TerradueCloudUsername)) owner.LoadCloudUsername();
                    var raiseOnBehalfOf = owner.TerradueCloudUsername;
                    var issue = new JiraServiceDeskIssueRequest {
                        serviceDeskId = context.GetConfigValue("jira-helpdesk-serviceDeskId"),
                        requestTypeId = context.GetConfigValue("jira-helpdesk-requestTypeId"),
                        raiseOnBehalfOf = raiseOnBehalfOf,
                        requestFieldValues = new JiraServiceDeskIssueFields {
                            summary = request.Subject,
                            description = request.Body,
                            components = components,
                            labels = labels
                        }
                    };
                    if (!string.IsNullOrEmpty(context.GetConfigValue("jira-helpdesk-customfield-ThematicAppLabel"))) {
                        issue.requestFieldValues.thematicAppLabels = new List<string> { request.AppId };
                    }
                    if (!string.IsNullOrEmpty(context.GetConfigValue("jira-helpdesk-customfield-ProcessingServiceLabel"))) {
                        if (job.Process != null && !string.IsNullOrEmpty(job.Process.Name)) {
                            var process = job.Process.Name.Replace(' ', '-');
                            issue.requestFieldValues.processingServicesLabels = new List<string> { process };
                        }
                    }
                    var jira = new JiraClient(context);
                    try {
                        jira.CreateServiceDeskIssue(issue);
                    }catch(Exception e){
                        issue.raiseOnBehalfOf = job.Owner.Email;
                        jira.CreateServiceDeskIssue(issue);
                    }
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Put(WpsJobUpdateNbResultsRequestTep request){
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            int nbresults = 0;
            try {
                context.Open();
                context.LogInfo(this, string.Format("/job/wps/{{identifier}}/nbresults PUT identifier='{0}'", request.JobId));

                WpsJob job = WpsJob.FromIdentifier(context, request.JobId);
                job.UpdateResultCount();
                nbresults = job.NbResults;
                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return nbresults;
        }

        public object Put(WpsJobUpdateArchiveStatusRequestTep request)
        {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebWpsJobTep result;
            try
            {
                context.Open();
                context.LogInfo(this, string.Format("/job/wps/{{identifier}}/archive PUT identifier='{0}', status={1}", request.JobId, request.ArchiveStatus));

                WpsJob job = WpsJob.FromIdentifier(context, request.JobId);
                job.ArchiveStatus = (WpsJobArchiveStatus)request.ArchiveStatus;
                job.Store();
                result = new WebWpsJobTep(job);
                context.Close();
            }
            catch (Exception e)
            {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            return result;
        }

        

    }
}

