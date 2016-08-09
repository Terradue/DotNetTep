using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/job/wps", "GET", Summary = "GET a list of WPS services", Notes = "")]
    public class WpsJobsGetRequestTep : IReturn<List<WebWpsJobTep>>{}

    [Route("/job/wps/{id}", "GET", Summary = "GET a WPS job", Notes = "")]
    public class WpsJobGetOneRequestTep : IReturn<WebWpsJobTep>{
        [ApiMember(Name="id", Description = "Id of the job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route("/job/wps", "POST", Summary = "POST a WPS job", Notes = "")]
    public class WpsJobCreateRequestTep : WebWpsJobTep, IReturn<WebWpsJobTep>{}

    [Route("/job/wps", "PUT", Summary = "POST a WPS job", Notes = "")]
    public class WpsJobUpdateRequestTep : WebWpsJobTep, IReturn<WebWpsJobTep>{}

    [Route("/job/wps/search", "GET", Summary = "GET WPS job as opensearch", Notes = "")]
    public class WpsJobSearchRequestTep : IReturn<HttpResult>{}

    [Route("/job/wps/description", "GET", Summary = "GET WPS job as opensearch", Notes = "")]
    public class WpsJobDescriptionRequestTep : IReturn<HttpResult>{}

//    [Route("/job/wps/{id}", "DELETE", Summary = "DELETE a WPS job", Notes = "")]
//    public class DeleteWPSJob : IReturn<WebWpsJob>{
//        [ApiMember(Name="id", Description = "Id of the job", ParameterType = "query", DataType = "int", IsRequired = true)]
//        public int id { get; set; }
//    }

    [Route("/job/wps/{id}", "DELETE", Summary = "DELETE a WPS job", Notes = "")]
    public class WpsJobDeleteRequestTep : IReturn<WebWpsJobTep>{
        [ApiMember(Name="id", Description = "Id of the job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string id { get; set; }
    }

    [Route("/job/wps/{jobId}/group", "GET", Summary = "GET list of groups that can access a job", Notes = "")]
    public class WpsJobGetGroupsRequestTep : IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "jobId", Description = "id of the service", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string JobId { get; set; }
    }

    [Route("/job/wps/{jobId}/group", "POST", Summary = "POST group to job", Notes = "")]
    public class WpsJobAddGroupRequestTep : WebGroup, IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "jobId", Description = "id of the service", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string JobId { get; set; }
    }

    [Route("/job/wps/{jobId}/group", "PUT", Summary = "PUT group to job", Notes = "")]
    public class WpsJobUpdateGroupsRequestTep : List<int>, IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "jobId", Description = "id of the job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string JobId { get; set; }
    }

    [Route("/job/wps/{jobId}/group/{Id}", "DELETE", Summary = "DELETE group to job", Notes = "")]
    public class WpsJobDeleteGroupRequestTep : IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "jobId", Description = "id of the wpsjob", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string JobId { get; set; }

        [ApiMember(Name = "Id", Description = "id of the group", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    public class WebWpsJobTep : WebEntity {
        [ApiMember(Name="RemoteIdentifier", Description = "RemoteIdentifier of the job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string RemoteIdentifier { get; set; }
        [ApiMember(Name="WpsId", Description = "Id of the WPS attached to the job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string WpsId { get; set; }
        [ApiMember(Name="Username", Description = "Name of the owner of the job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Username { get; set; }
        [ApiMember(Name="ProcessId", Description = "Process ID attached to the job", ParameterType = "query", DataType = "String", IsRequired = true)]
        public String ProcessId { get; set; }
        [ApiMember(Name="ProcessName", Description = "Process name attached to the job", ParameterType = "query", DataType = "String", IsRequired = true)]
        public String ProcessName { get; set; }
        [ApiMember(Name="StatusLocation", Description = "Status location of the job", ParameterType = "query", DataType = "String", IsRequired = true)]
        public String StatusLocation { get; set; }
        [ApiMember(Name="CreatedTime", Description = "Created time of the job", ParameterType = "query", DataType = "DateTime", IsRequired = true)]
        public DateTime CreatedTime { get; set; }
        [ApiMember(Name="Parameters", Description = "Parameters attached to the job", ParameterType = "query", DataType = "List<KeyValuePair<string, string>>", IsRequired = true)]
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public WebWpsJobTep() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebWpsJob"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebWpsJobTep(WpsJob entity, IfyContext context = null) : base(entity) {
            this.WpsId = entity.WpsId;
            
            this.ProcessId = entity.ProcessId;
            if (context != null) {
                try {
                    this.ProcessName = Service.FromIdentifier(context, entity.ProcessId).Name;
                } catch (Exception) {
                }
                try{
                    this.Username = User.FromId(context, entity.OwnerId).Username;
                }catch(Exception){}
            }
            
            this.StatusLocation = entity.StatusLocation;
            this.Parameters = entity.Parameters;
            this.CreatedTime = entity.CreatedTime;
            this.RemoteIdentifier = entity.RemoteIdentifier;
                       
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
        /// <param name="input">Input.</param>
        public WpsJob ToEntity(IfyContext context, WpsJob input){
            WpsJob entity = (input == null ? new WpsJob(context) : input);

            entity.Identifier = this.Identifier;
			entity.Name = this.Name;
            entity.OwnerId = context.UserId;
            entity.ProcessId = this.ProcessId;
            if (String.IsNullOrEmpty(this.WpsId) && !String.IsNullOrEmpty(this.ProcessId)) {
                entity.WpsId = entity.Process.Provider.Identifier;
            }
            else entity.WpsId = this.WpsId;
            entity.Parameters = this.Parameters;
            entity.StatusLocation = this.StatusLocation;
            entity.CreatedTime = this.CreatedTime;
            return entity;
        }

    }
}

