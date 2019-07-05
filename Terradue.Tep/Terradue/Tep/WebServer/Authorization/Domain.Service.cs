using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services
{
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
                domains.AddSort("Kind", SortDirection.Descending);
                domains.AddSort("Name", SortDirection.Ascending);
                domains.Load();

                foreach (var domain in domains) result.Add (new WebDomain (domain));

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
                domain.Identifier = TepUtility.ValidateIdentifier(domain.Identifier);
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

        public object Get (DomainSearchRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            context.Open ();
            context.LogInfo (this, string.Format ("/domain/search GET"));

            EntityList<Domain> domains = new EntityList<Domain> (context);
            domains.Load ();

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

        public object Get (DomainDescriptionRequestTep request)
        {
            var context = TepWebContext.GetWebContext (PagePrivileges.EverybodyView);
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/domain/description GET"));

                EntityList<Domain> domains = new EntityList<Domain> (context);
                domains.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = domains.GetOpenSearchDescription ();

                context.Close ();

                return new HttpResult (osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }
        }

        public object Post (UploadDomainImageRequest request){
            var context = TepWebContext.GetWebContext (PagePrivileges.AdminOnly);
            string img = "";
            string uid = Guid.NewGuid ().ToString ();
            string extension = ".png";

            WebDomain result = null;

            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/domain/{{Id}}/image POST Id='{0}'", request.Id));

                if (request.Id == 0) {
                    var segments = base.Request.PathInfo.Split (new [] { '/' },
                                                               StringSplitOptions.RemoveEmptyEntries);
                    request.Id = System.Int32.Parse (segments [1]);
                }

                Terradue.Portal.Domain domain = Terradue.Portal.Domain.FromId (context, request.Id);
                string oldImg = domain.IconUrl;
                if (this.RequestContext.Files.Length > 0) {
                    var uploadedFile = this.RequestContext.Files [0];
                    extension = uploadedFile.FileName.Substring (uploadedFile.FileName.LastIndexOf ("."));
                    img = "/files/" + uid + extension;

                    string path = AppDomain.CurrentDomain.BaseDirectory;
                    if (!path.EndsWith ("/")) path += "/";

                    context.LogInfo (this, string.Format ("Uploading image to {0}", path + img));
                    uploadedFile.SaveTo (path + img);
                } else {
                    using (var fileStream = File.Create ("files/" + uid + extension)) {
                        img = "files/" + uid + extension;
                        request.RequestStream.CopyTo (fileStream);
                    }
                }
                domain.IconUrl = img;
                domain.Store ();

                result = new WebDomain (domain);

                try {
                    if (oldImg != null) File.Delete ("files/" + oldImg);
                } catch (Exception) { }

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post (CreateDomainOwnerRequest request){
            var context = TepWebContext.GetWebContext (PagePrivileges.AdminOnly);
            WebDomain result;
            try {
                context.Open ();
                context.LogInfo (this, string.Format ("/domain/{{id}}/owner POST Id='{0}', UserId='{1}'", request.Id, request.UserId));
                ThematicCommunity domain = ThematicCommunity.FromId (context, request.Id);
                UserTep owner = UserTep.FromId (context, request.UserId);
                domain.SetOwner (owner);

                result = new WebDomain (domain);

                context.Close ();
            } catch (Exception e) {
                context.LogError (this, e.Message);
                context.Close ();
                throw e;
            }
            return result;
        }

        //public object Put (UpdateDomainUserRequest request)
        //{
        //    var context = TepWebContext.GetWebContext (PagePrivileges.UserView);
        //    try {
        //        context.Open ();
        //        context.LogInfo (this, string.Format ("/domain/{{id}}/user PUT Id='{0}', UserId='{1}', RoleId='{2}'", request.Id, request.UserId, request.RoleId));
        //        Role role = Role.FromId (context, request.RoleId);
        //        Domain domain = request.Id != 0 ? Domain.FromId (context, request.Id) : null;
        //        if (request.UserId != 0) {
        //            User usr = User.FromId (context, request.UserId);
        //            role.GrantToUser (usr, domain);
        //            context.LogDebug (this, string.Format ("Role {0} granted for user {1} for domain {2}", role.Identifier, usr.Username, domain != null ? domain.Name : "n/a"));
        //        } else if (request.GroupId != 0) {
        //            if (request.UserId != 0) throw new Exception ("Select only one amongst User and Group");
        //            Group grp = Group.FromId (context, request.GroupId);
        //            role.GrantToGroup (grp, domain);
        //            context.LogDebug (this, string.Format ("Role {0} granted for group {1} for domain {2}", role.Identifier, grp.Identifier, domain != null ? domain.Name : "n/a"));
        //        } else throw new Exception ("Select one amongst User and Group");

        //        context.Close ();
        //    } catch (Exception e) {
        //        context.LogError (this, e.Message);
        //        context.Close ();
        //        throw e;
        //    }
        //    return new WebResponseBool (true);
        //}

    }
}

