using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Artifactory.Response;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

    [Route("/store", "GET", Summary = "GET root folder", Notes = "")]
    public class GetStorageRepoRequestTep : IReturn<HttpResult> {
        [ApiMember(Name = "apikey", Description = "api key", ParameterType = "quert", DataType = "string", IsRequired = false)]
        public string apikey { get; set; }
    }

    [Route("/store/{repoKey}/{path*}", "GET", Summary = "GET folder and files", Notes = "")]
    public class GetStorageFilesRequestTep : IReturn<HttpResult> {
        [ApiMember(Name = "repoKey", Description = "repo Key", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string repoKey { get; set; }

        [ApiMember(Name = "path", Description = "path", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string path { get; set; }

        [ApiMember(Name = "apikey", Description = "api key", ParameterType = "quert", DataType = "string", IsRequired = false)]
        public string apikey { get; set; }
    }

    [Route("/store/download/{repoKey}/{path*}", "GET", Summary = "GET file", Notes = "")]
    public class GetDownloadStorageFileRequestTep : IReturn<HttpResult> {
        [ApiMember(Name = "repoKey", Description = "repo Key", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string repoKey { get; set; }

        [ApiMember(Name = "path", Description = "path", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string path { get; set; }

        [ApiMember(Name = "apikey", Description = "api key", ParameterType = "quert", DataType = "string", IsRequired = false)]
        public string apikey { get; set; }
    }

    [Route("/store/{repoKey}/{path*}", "PUT", Summary = "PUT folder and files", Notes = "")]
    public class PutStorageFolderRequestTep : IReturn<HttpResult> {
        [ApiMember(Name = "repoKey", Description = "repo Key", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string repoKey { get; set; }

        [ApiMember(Name = "path", Description = "path", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string path { get; set; }

        [ApiMember(Name = "apikey", Description = "api key", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string apikey { get; set; }
    }

    [Route("/store/move/{srcRepoKey}/{srcPath*}", "PUT", Summary = "PUT folder and files", Notes = "")]
    public class PutMoveStorageItemRequestTep : IReturn<HttpResult> {
        [ApiMember(Name = "srcRepoKey", Description = "src repo Key", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string srcRepoKey { get; set; }

        [ApiMember(Name = "srcPath", Description = "src path", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string srcPath { get; set; }

        [ApiMember(Name = "to", Description = "to repo and path", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string to { get; set; }

        [ApiMember(Name = "dry", Description = "dry run", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int dry { get; set; }

        [ApiMember(Name = "apikey", Description = "api key", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string apikey { get; set; }
    }

    [Route("/store/{repoKey}/{path*}", "DELETE", Summary = "DELETE file", Notes = "")]
    public class DeleteStorageFileRequestTep : IReturn<HttpResult> {
        [ApiMember(Name = "repoKey", Description = "repo Key", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string repoKey { get; set; }

        [ApiMember(Name = "path", Description = "path", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string path { get; set; }

        [ApiMember(Name = "apikey", Description = "api key", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string apikey { get; set; }
    }

    [Route("/store/{repoKey}/{path*}", "POST", Summary = "POST the processor package", Notes = "")]
    public class PostStorageFilesRequestTep : IRequiresRequestStream, IReturn<HttpResult> {
        public System.IO.Stream RequestStream { get; set; }

        [ApiMember(Name = "repoKey", Description = "repo Key", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string repoKey { get; set; }

        [ApiMember(Name = "path", Description = "path", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string path { get; set; }

        [ApiMember(Name = "apikey", Description = "api key", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string apikey { get; set; }
    }

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
             EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class StorageServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetStorageRepoRequestTep request) {
            var context = string.IsNullOrEmpty(request.apikey) ? TepWebContext.GetWebContext(PagePrivileges.UserView) : TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            string result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/store GET"));

                var apikey = request.apikey ?? UserTep.FromId(context, context.UserId).GetSessionApiKey();
                var factory = new StoreFactory(context, apikey);

                RepositoryInfoList repos = factory.GetRepositoriesToDeploy();
                var children = new List<FileInfoChildren>();
                if (repos != null && repos.RepoTypesList != null) {
                    foreach (var repo in repos.RepoTypesList) {
                        var child = new FileInfoChildren {
                            Uri = "/" + repo.RepoKey,
                            Folder = true
                        };
                        children.Add(child);
                    }
                }
                FolderInfo info = new FolderInfo {
                    Uri = System.Web.HttpContext.Current.Request.Url.AbsoluteUri,
                    Repo = "",
                    Path = "/",
                    Children = children.ToArray()
                };
                result = factory.Serializer.Serialize(info);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return new HttpResult(result, Request.ContentType);
        }

        public object Get(GetStorageFilesRequestTep request) {
            var context = string.IsNullOrEmpty(request.apikey) ? TepWebContext.GetWebContext(PagePrivileges.UserView) : TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            string result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/store/{0}/{1} GET",request.repoKey, request.path));

                var apikey = request.apikey ?? UserTep.FromId(context, context.UserId).GetSessionApiKey();
                var factory = new StoreFactory(context, apikey);

                FolderInfo info = factory.GetFolderInfo(request.repoKey, request.path);
                info.Uri = System.Web.HttpContext.Current.Request.Url.AbsoluteUri;
                result = factory.Serializer.Serialize(info);

                context.Close();
            }catch(Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return new HttpResult(result, Request.ContentType);
        }

        public object Get(GetDownloadStorageFileRequestTep request) {
            var context = string.IsNullOrEmpty(request.apikey) ? TepWebContext.GetWebContext(PagePrivileges.UserView) : TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            Stream result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/store/download/{0}/{1} GET", request.repoKey, request.path));

                var apikey = request.apikey ?? UserTep.FromId(context, context.UserId).GetSessionApiKey();
                var factory = new StoreFactory(context, apikey);

                result = factory.DownloadItem(request.repoKey, request.path);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            var filename = request.path.Substring(request.path.LastIndexOf('/') + 1);

            Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}", filename));
            return new HttpResult(result, "application/octet");
        }

        public object Put(PutStorageFolderRequestTep request) {
            var context = string.IsNullOrEmpty(request.apikey) ? TepWebContext.GetWebContext(PagePrivileges.UserView) : TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            string result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/store/{0}/{1} PUT", request.repoKey, request.path));

                var apikey = request.apikey ?? UserTep.FromId(context, context.UserId).GetSessionApiKey();
                var factory = new StoreFactory(context, apikey);

                var info = factory.CreateFolder(request.repoKey, request.path);
                result = factory.Serializer.Serialize(info);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return new HttpResult(result, Request.ContentType);
        }

        public object Put(PutMoveStorageItemRequestTep request) {
            var context = string.IsNullOrEmpty(request.apikey) ? TepWebContext.GetWebContext(PagePrivileges.UserView) : TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            string result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/store/move/{0}/{1}?to={2}&dry={3} PUT", request.srcRepoKey, request.srcPath, request.to, request.dry));

                var apikey = request.apikey ?? UserTep.FromId(context, context.UserId).GetSessionApiKey();
                var factory = new StoreFactory(context, apikey);

                var to = request.to.Trim('/');
                var toRepo = to.Substring(0, to.IndexOf('/'));
                var toPath = to.Substring(to.IndexOf('/') + 1);

                var info = factory.MoveItem(request.srcRepoKey, request.srcPath, toRepo, toPath, request.dry);
                result = factory.Serializer.Serialize(info);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return new HttpResult(result, Request.ContentType);
        }

        public object Delete(DeleteStorageFileRequestTep request) {
            var context = string.IsNullOrEmpty(request.apikey) ? TepWebContext.GetWebContext(PagePrivileges.UserView) : TepWebContext.GetWebContext(PagePrivileges.EverybodyView);

            try {
                context.Open();
                context.LogInfo(this, string.Format("/store/{0}/{1} DELETE", request.repoKey, request.path));

                var apikey = request.apikey ?? UserTep.FromId(context, context.UserId).GetSessionApiKey();
                var factory = new StoreFactory(context, apikey);

                factory.DeleteFile(request.repoKey, request.path);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return new WebResponseBool(true);
        }

        public object Post(PostStorageFilesRequestTep request) {

            if (request.repoKey == null) {
                var segments = base.Request.PathInfo.Split(new[] { '/' },StringSplitOptions.RemoveEmptyEntries);
                request.repoKey = segments[1];
                request.path = base.Request.PathInfo.Substring(base.Request.PathInfo.IndexOf(request.repoKey + "/") + request.repoKey.Length + 1);
            }
            if (request.apikey == null) {
                request.apikey = base.Request.QueryString["apikey"];
            }

            var context = string.IsNullOrEmpty(request.apikey) ? TepWebContext.GetWebContext(PagePrivileges.UserView) : TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.LogInfo(this, string.Format("/store/{0}/{1} POST", request.repoKey, request.path));

            string path = System.Configuration.ConfigurationManager.AppSettings["UploadTmpPath"] ?? "/tmp";

            try {
                context.Open();

                var apikey = request.apikey ?? UserTep.FromId(context, context.UserId).GetSessionApiKey();
                var factory = new StoreFactory(context, apikey);

                var filename = path + "/" + request.path.Substring(request.path.LastIndexOf("/") + 1);
                using (var stream = new MemoryStream()) {
                    if (this.RequestContext.Files.Length > 0) {
                        var uploadedFile = this.RequestContext.Files[0];
                        uploadedFile.SaveTo(filename);
                    } else {
                        using (var fileStream = File.Create(filename)) {
                            request.RequestStream.CopyTo(fileStream);
                        }
                    }
                    factory.UploadFile(request.repoKey, request.path, filename);
                }
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }
            context.Close();
            return new WebResponseBool(true);
        }

    }
}