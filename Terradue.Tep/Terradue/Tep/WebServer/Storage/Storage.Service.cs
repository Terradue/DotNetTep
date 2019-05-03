using System;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Artifactory.Response;
using Terradue.Portal;

namespace Terradue.Tep.WebServer.Services {

    [Route("/storage/{repoKey}/{path*}", "GET", Summary = "GET folder and files", Notes = "")]
    public class GetStorageFilesRequestTep {
        [ApiMember(Name = "repoKey", Description = "repo Key", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string repoKey { get; set; }

        [ApiMember(Name = "path", Description = "path", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string path { get; set; }
    }

    [Route("/storage/{repoKey}/{path*}", "DELETE", Summary = "DELETE file", Notes = "")]
    public class DeleteStorageFileRequestTep {
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

    }
}