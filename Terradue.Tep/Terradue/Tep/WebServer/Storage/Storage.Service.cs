using System;
using System.IO;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Artifactory.Response;
using Terradue.Portal;

namespace Terradue.Tep.WebServer.Services {

    [Route("/storage/{repoKey}/{path*}", "GET", Summary = "GET folder and files", Notes = "")]
    public class GetStorageFilesRequestTep : IReturn<HttpResult> {
        [ApiMember(Name = "repoKey", Description = "repo Key", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string repoKey { get; set; }

        [ApiMember(Name = "path", Description = "path", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string path { get; set; }
    }

    [Route("/storage/{repoKey}/{path*}", "DELETE", Summary = "DELETE file", Notes = "")]
    public class DeleteStorageFileRequestTep : IReturn<HttpResult> {
        [ApiMember(Name = "repoKey", Description = "repo Key", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string repoKey { get; set; }

        [ApiMember(Name = "path", Description = "path", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string path { get; set; }
    }

    [Route("/storage/{repoKey}/{path*}", "POST", Summary = "POST the processor package", Notes = "")]
    public class PostStorageFilesRequestTep : IRequiresRequestStream, IReturn<HttpResult> {
        [ApiMember(Name = "RequestStream", Description = "RequestStream", ParameterType = "query", DataType = "Stream", IsRequired = false)]
        public System.IO.Stream RequestStream { get; set; }

        [ApiMember(Name = "repoKey", Description = "repo Key", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string repoKey { get; set; }

        [ApiMember(Name = "path", Description = "path", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string path { get; set; }
    }

    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
             EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class StorageServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetStorageFilesRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            string result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/storage/{0}/{1} GET",request.repoKey, request.path));

                var user = UserTep.FromId(context, context.UserId);
                var factory = new StoreFactory(user.GetSessionApiKey());

                FolderInfo info = factory.GetFolderInfo(request.repoKey, request.path);
                result = factory.Serializer.Serialize(info);

                context.Close();
            }catch(Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return new HttpResult(result);
        }

        public object Delete(DeleteStorageFileRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            string result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/storage/{0}/{1} DELETE", request.repoKey, request.path));

                var user = UserTep.FromId(context, context.UserId);
                var factory = new StoreFactory(user.GetSessionApiKey());

                FolderInfo info = factory.GetFolderInfo(request.repoKey, request.path);
                result = factory.Serializer.Serialize(info);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message + " - " + e.StackTrace);
                context.Close();
                throw e;
            }

            return new HttpResult(result);
        }

        public object Post(PostStorageFilesRequestTep request) {
            IfyWebContext context;
            context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            context.LogInfo(this, string.Format("/storage/{0}/{1} POST", request.repoKey, request.path));

            string path = System.Configuration.ConfigurationManager.AppSettings["UploadTmpPath"] ?? "/tmp";

            try {
                context.Open();

                var user = UserTep.FromId(context, context.UserId);
                var factory = new StoreFactory(user.GetSessionApiKey());

                var filename = path + "/" + Guid.NewGuid().ToString() + ".zip";
                using (var stream = new MemoryStream()) {
                    if (this.RequestContext.Files.Length > 0) {
                        var uploadedFile = this.RequestContext.Files[0];
                        filename = path + "/" + this.RequestContext.Files[0].FileName;
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