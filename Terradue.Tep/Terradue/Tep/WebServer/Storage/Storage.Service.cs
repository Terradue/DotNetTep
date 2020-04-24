using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Artifactory.Response;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

    /*
     swagger: "2.0"
info:
  description: "Terradue store API"
  version: "1.0.0"
  title: "T2 store API"
  termsOfService: "http://swagger.io/terms/"
  contact:
    email: "info@terradue.com"
  license:
    name: "Apache 2.0"
    url: "http://www.apache.org/licenses/LICENSE-2.0.html"
host: "ellip.terradue.com"
basePath: "/v2"
tags:
- name: "store"
  description: "All requests to handle items on the Terradue storage"
  externalDocs:
    description: "Find out more"
    url: "http://swagger.io"
schemes:
- "https"
paths:
  /store/{path*}:
    get:
      tags:
      - "store"
      summary: "List all folder and items under {repo}/{path}"
      description: ""
      operationId: "GetStorageFilesRequestTep"
      produces:
      - "application/json"
      parameters:
      - in: "path"
        name: "path*"
        description: "Path to store item"
        required: true
        type: string
        x-example: path/to/item
      - in: "query"
        name: "apikey"
        description: "User apikey (if not set, user must be logged in)"
        required: false
        type: string
        x-example: Akcdfghjflgnljxfgnlkjfnbmlknlnmk
      responses:
        200:
          description: "Repositories info"
          schema:
            $ref: "#/definitions/FolderInfo"
        500:
          description: "Invalid input"
    post:
      tags:
      - "store"
      summary: "Upload item"
      description: "Note: the file name should be included into path"
      operationId: "PostStorageFilesRequestTep"
      produces:
      - "application/json"
      parameters:
      - in: "path"
        name: "path*"
        description: "Path to store item"
        required: true
        type: string
        x-example: path/to/item
      - in: "query"
        name: "apikey"
        description: "User apikey (if not set, user must be logged in)"
        required: false
        type: string
        x-example: Akcdfghjflgnljxfgnlkjfnbmlknlnmk
      responses:
        200:
          description: "Upload succeeded"
          schema:
            $ref: "#/definitions/ResponseBool"
        405:
          description: "Invalid input"
    delete:
      tags:
      - "store"
      summary: "Delete item or folder"
      description: ""
      operationId: "DeleteStorageFileRequestTep"
      produces:
      - "application/json"
      parameters:
      - in: "path"
        name: "path*"
        description: "Path to store item"
        required: true
        type: string
        x-example: path/to/item
      - in: "query"
        name: "apikey"
        description: "User apikey (if not set, user must be logged in)"
        required: false
        type: string
        x-example: Akcdfghjflgnljxfgnlkjfnbmlknlnmk
      responses:
        200:
          description: "Delete succeeded"
          schema:
            $ref: "#/definitions/ResponseBool"
        405:
          description: "Invalid input"
  /store/download/{path*}:
    get:
      tags:
      - "store"
      summary: "Download item"
      description: ""
      operationId: "GetDownloadStorageFileRequestTep"
      produces:
      - "application/octet+stream"
      parameters:
      - in: "path"
        name: "path*"
        description: "Path to store item"
        required: true
        type: string
        x-example: path/to/item
      - in: "query"
        name: "apikey"
        description: "User apikey (if not set, user must be logged in)"
        required: false
        type: string
        x-example: Akcdfghjflgnljxfgnlkjfnbmlknlnmk
      responses:
        200:
          description: "Item as octet stream"
        500:
          description: "Invalid input"
  /store/move/{path*}:
    put:
      tags:
      - "store"
      summary: "Move item"
      description: ""
      operationId: "PutMoveStorageItemRequestTep"
      produces:
      - "application/json"
      parameters:
      - in: "path"
        name: "path*"
        description: "Path to store item"
        required: true
        type: string
        x-example: path/to/item
      - in: "query"
        name: "to"
        description: "Path where to move the item"
        required: false
        type: string
        x-example: path/to/new/item
      - in: "query"
        name: "dry"
        description: "Dry run -> 1 (the action is not performed, but an error is return if not possible)"
        required: false
        type: integer
        x-example: 1
      - in: "query"
        name: "apikey"
        description: "User apikey (if not set, user must be logged in)"
        required: false
        type: string
        x-example: Akcdfghjflgnljxfgnlkjfnbmlknlnmk
      responses:
        200:
          description: "Move succeeded"
          schema:
            $ref: "#/definitions/MessageContainer"
        500:
          description: "Invalid input"
definitions:
  FolderInfo:
    type: "object"
    properties:
      uri:
        type: "string"
      repo:
        type: "string"
      path:
        type: "string"
      relativePath:
        type: "string"
      lastModified:
        type: "string"
        format: "date-time"
      createdBy:
        type: "string"
      modifiedBy:
        type: "string"
      created:
        type: "string"
        format: "date-time"
      lastUpdated:
        type: "string"
        format: "date-time"
      Children:
        type: array
        items:
          $ref: "#/definitions/FileInfoChildren"
  FileInfoChildren:
    type: "object"
    properties:
      folder:
        type: "boolean"
      size:
        type: "number"
      lastModified:
        type: "string"
      sha1:
        type: "string"
  MessageContainer:
    type: "object"
    properties:
      messages:
        type: array
        items:
          $ref: "#/definitions/Message"
      errors:
        type: array
        items:
          $ref: "#/definitions/Message"
  Message:
    type: "object"
    properties:
      level:
        type: "string"
      status:
        type: "string"
      message:
        type: "string"
  ResponseBool:
    type: "object"
    properties:
      Response:
        type: "string"
externalDocs:
  description: "Find out more about Swagger"
  url: "http://swagger.io"   
    */

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
                context.LogError(this, e.Message, e);
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

                Artifactory.Response.FileInfo info = factory.GetItemInfo(request.repoKey, request.path);
                info.Uri = System.Web.HttpContext.Current.Request.Url.AbsoluteUri;
                result = factory.Serializer.Serialize(info);

                context.Close();
            }catch(Exception e) {
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }

            return new HttpResult(result, Request.ContentType);
        }

        public object Get(GetDownloadStorageFileRequestTep request) {
            //var context = string.IsNullOrEmpty(request.apikey) ? TepWebContext.GetWebContext(PagePrivileges.UserView) : TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            Stream result = null;

            try {
                context.Open();
                context.LogInfo(this, string.Format("/store/download/{0}/{1} GET", request.repoKey, request.path));

                var apikey = request.apikey ?? (context.UserId > 0 ? UserTep.FromId(context, context.UserId).GetSessionApiKey() : null);
                var factory = new StoreFactory(context, apikey);

                result = factory.DownloadItem(request.repoKey, request.path);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
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
                context.LogError(this, e.Message, e);
                context.Close();
                throw e;
            }
            context.Close();
            return new WebResponseBool(true);
        }

    }
}