using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/wps/WebProcessingService", "POST", Summary = "POST the wps", Notes = "")]
    public class WpsExecutePostRequestTep : IRequiresRequestStream, IReturn<HttpResult> {
        public System.IO.Stream RequestStream { get; set; }
    }

    [Route("/service/wps/{Id}", "GET", Summary = "GET a WPS service", Notes = "")]
    public class WpsServiceGetRequestTep : IReturn<WebServiceTep> {
        [ApiMember(Name = "Id", Description = "Service id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route("/service/wps", "PUT", Summary = "PUT update service", Notes = "")]
    public class WpsServiceUpdateRequestTep : WebServiceTep {

        [ApiMember(Name = "access", Description = "Define if the service shall be public or private", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Access { get; set; }

    }

    [Route("/service/wps/tags", "GET", Summary = "GET all existing services tags", Notes = "")]
    public class WpsServiceAllTagsRequestTep : List<string> {}

    [Route("/service/wps/versions", "GET", Summary = "GET all existing service versions", Notes = "")]
    public class WpsServiceAllVersionsRequestTep : List<Version> { }

    [Route("/service/wps/tags", "PUT", Summary = "PUT update service tags", Notes = "")]
    public class WpsServiceUpdateTagsRequestTep : WebServiceTep { }

    [Route("/service/wps/{identifier}/available", "PUT", Summary = "PUT update service availability", Notes = "")]
    public class WpsServiceUpdateAvailabilityRequestTep {
        [ApiMember(Name = "Identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/service/wps/{wpsId}/group", "GET", Summary = "GET list of groups that can access a service", Notes = "")]
    public class WPSServiceGetGroupsRequestTep : IReturn<List<WebGroup>> {
        [ApiMember(Name = "wpsId", Description = "id of the service", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int WpsId { get; set; }
    }

    [Route("/service/wps/{wpsId}/group", "POST", Summary = "POST group to service", Notes = "")]
    public class WpsServiceAddGroupRequestTep : WebGroup, IReturn<List<WebGroup>> {
        [ApiMember(Name = "wpsId", Description = "id of the service", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int WpsId { get; set; }
    }

    [Route("/service/wps/{wpsId}/group/{Id}", "DELETE", Summary = "DELETE group to service", Notes = "")]
    public class WpsServiceDeleteGroupRequestTep : IReturn<List<WebGroup>> {
        [ApiMember(Name = "wpsId", Description = "id of the service", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int WpsId { get; set; }

        [ApiMember(Name = "Id", Description = "id of the group", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route("/cr/wps/{Identifier}/services", "GET", Summary = "GET a list of WPS services", Notes = "")]
    public class GetWPSProvidersServices : IReturn<List<WebServiceTep>> {
        [ApiMember(Name = "Identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/cr/wps/{CrIdentifier}/services", "POST", Summary = "POST a WPS service", Notes = "")]
    public class CreateWPSProvidersService : WebServiceTep, IReturn<List<WebServiceTep>> {
        [ApiMember(Name = "CrIdentifier", Description = "Computing resource Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string CrIdentifier { get; set; }
    }

    [Route("/service/wps/{Identifier}/versions", "GET", Summary = "GET a list of WPS services", Notes = "")]
    public class GetWPSServicesVersions : IReturn<List<WebServiceTep>> {
        [ApiMember(Name = "Identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/service/wps/{OldIdentifier}/replace", "PUT", Summary = "PUT a WPS services in place of another", Notes = "")]
    public class ReplaceWPSService : WebServiceTep, IReturn<WebServiceTep> {
        [ApiMember(Name = "Identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string OldIdentifier { get; set; }

        [ApiMember(Name = "deleteold", Description = "Indicates if old WPS service should be removed", ParameterType = "query", DataType = "bool", IsRequired = true)]
        public bool DeleteOld { get; set; }
    }

    [Route("/cr/wps/{Identifier}/devusers", "GET", Summary = "GET a WPS provider dev users", Notes = "")]
    public class GetWpsProviderDevUsers : IReturn<List<WebUser>> {
        [ApiMember(Name = "Identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/cr/wps/{Identifier}/devusers", "PUT", Summary = "PUT a WPS provider dev user", Notes = "")]
    public class AddWpsProviderDevUsers : IReturn<WebUser> {
        [ApiMember(Name = "Username", Description = "Dev users usernames", ParameterType = "query", DataType = "List<string>", IsRequired = true)]
        public string Username { get; set; }

        [ApiMember(Name = "Identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/cr/wps/{Identifier}/devusers/{Username}", "DELETE", Summary = "PUT a WPS provider dev user", Notes = "")]
    public class RemoveWpsProviderDevUsers : IReturn<WebUser> {
        [ApiMember(Name = "Username", Description = "Dev users usernames", ParameterType = "query", DataType = "List<string>", IsRequired = true)]
        public string Username { get; set; }

        [ApiMember(Name = "Identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/cr/wps/{Identifier}/sync", "GET", Summary = "GET refresh wps provider", Notes = "")]
    public class WpsProviderSyncForUserRequestTep : IReturn<WebResponseBool> {
        [ApiMember(Name = "Username", Description = "Dev users usernames", ParameterType = "query", DataType = "List<string>", IsRequired = true)]
        public string Username { get; set; }

        [ApiMember(Name = "Identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/user/current/service/wps/sync", "GET", Summary = "GET refresh current user wps service", Notes = "")]
    public class CurrentUserWpsServiceSyncRequestTep: IReturn<WebResponseBool> { }

    public class WebServiceTep : Terradue.WebService.Model.WebWpsService {

        [ApiMember(Name = "IsPublic", Description = "IsPublic", ParameterType = "path", DataType = "bool", IsRequired = false)]
        public bool IsPublic { get; set; }
        [ApiMember(Name = "OwnerId", Description = "Owner Id", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int OwnerId { get; set; }
        [ApiMember(Name = "Provider", Description = "Provider name", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Provider { get; set; }
        [ApiMember(Name = "Geometry", Description = "Geometry", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string Geometry { get; set; }
        [ApiMember(Name = "Commercial", Description = "Commercial", ParameterType = "path", DataType = "bool", IsRequired = false)]
        public bool Commercial { get; set; }

        public WebServiceTep() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WebServer.WebServiceTep"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="entity">Entity.</param>
        public WebServiceTep(IfyContext context, WpsProcessOffering entity) : base(entity) {
            this.IsPublic = entity.DoesGrantPermissionsToAll();
            this.OwnerId = entity.UserId;
            this.Geometry = entity.Geometry;
            this.Commercial = entity.Commercial;
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
        public new WpsProcessOffering ToEntity(IfyContext context, WpsProcessOffering input) {

            WpsProcessOffering entity = base.ToEntity(context, input);
            entity.Geometry = this.Geometry;
            entity.Commercial = this.Commercial;
            return entity;
        }

    }
}


