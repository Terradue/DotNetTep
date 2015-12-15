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
    public class WpsServiceGetRequestTep : IReturn<WebServiceTep>{
        [ApiMember(Name="Id", Description = "Service id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route("/service/wps", "PUT", Summary = "PUT update service", Notes = "")]
    public class WpsServiceUpdateRequestTep : WebServiceTep, IReturn<List<WebGroup>> {

        [ApiMember(Name = "access", Description = "Define if the service shall be public or private", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Access { get; set; }

    }

    [Route("/service/wps/{wpsId}/group", "GET", Summary = "GET list of groups that can access a service", Notes = "")]
    public class WPSServiceGetGroupsRequestTep : IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "wpsId", Description = "id of the service", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int WpsId { get; set; }
    }

    [Route("/service/wps/{wpsId}/group", "POST", Summary = "POST group to service", Notes = "")]
    public class WpsServiceAddGroupRequestTep : WebGroup, IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "wpsId", Description = "id of the service", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int WpsId { get; set; }
    }

    [Route("/service/wps/{wpsId}/group/{Id}", "DELETE", Summary = "DELETE group to service", Notes = "")]
    public class WpsServiceDeleteGroupRequestTep : IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "wpsId", Description = "id of the service", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int WpsId { get; set; }

        [ApiMember(Name = "Id", Description = "id of the group", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    public class WebServiceTep : Terradue.WebService.Model.WebService {

        [ApiMember(Name="IsPublic", Description = "Remote resource IsPublic", ParameterType = "path", DataType = "bool", IsRequired = false)]
        public bool IsPublic { get; set; }

        public WebServiceTep() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebDataPackageTep"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebServiceTep(IfyContext context, Service entity) : base(entity)
        {
            this.IsPublic = entity.HasGlobalPrivilege();
        }

    }
}


