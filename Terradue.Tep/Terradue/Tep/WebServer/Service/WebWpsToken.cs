using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {


    [Route("/service/wps/{Identifier}/token", "GET", Summary = "GET a list of WPS services", Notes = "")]
    public class GetWPSServiceTokens : IReturn<List<WebWpsToken>> {
        [ApiMember(Name = "Identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/service/wps/{ServiceIdentifier}/token", "POST", Summary = "GET a list of WPS services", Notes = "")]
    public class PostWPSServiceToken : WebWpsToken, IReturn<WebWpsToken> {}

    [Route("/service/wps/{ServiceIdentifier}/token", "POST", Summary = "GET a list of WPS services", Notes = "")]
    public class PutWPSServiceToken : WebWpsToken, IReturn<WebWpsToken> {}

    [Route("/wps/token/{Id}", "DELETE", Summary = "DELETE a WPS token", Notes = "")]
    public class DeleteWPSServiceToken : IReturn<WebResponseBool> {
        [ApiMember(Name = "Id", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }
    }

    public class WebWpsToken: WebEntity {
        
        [ApiMember(Name="ServiceId", Description = "Id of the WPS attached to the token", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int ServiceId { get; set; }        
        [ApiMember(Name="ServiceIdentifier", Description = "Identifier of the WPS attached to the token", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string ServiceIdentifier { get; set; }        
        [ApiMember(Name="GroupId", Description = "Id of the group attached to the token", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int GroupId { get; set; }  
        [ApiMember(Name="UserId", Description = "Id of the user attached to the token", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int UserId { get; set; }  
        [ApiMember(Name="Groupname", Description = "Name of the group attached to the token", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Groupname { get; set; }  
        [ApiMember(Name="Username", Description = "Name of the User attached to the token", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Username { get; set; }  
        [ApiMember(Name = "EndTime", Description = "End time of the token", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string EndTime { get; set; }
        [ApiMember(Name = "NbInputs", Description = "NbInputs remaining for the token", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int NbInputs { get; set; }
        [ApiMember(Name = "NbMax", Description = "NbMax inputs for the token", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int NbMax { get; set; }        

        public WebWpsToken() {}
        
        public WebWpsToken(WpsToken entity, IfyContext context = null) : base(entity) {
            this.ServiceId = entity.ServiceId;
            this.UserId = entity.OwnerId;    
            this.Username = entity.Username;
            this.Groupname = entity.Groupname;
            this.EndTime = entity.EndTime.ToString("yyyy-MM-dd");
            this.NbInputs = entity.NbInputs;
            this.NbMax = entity.NbMax;
            
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
        /// <param name="input">Input.</param>
        public WpsToken ToEntity(IfyContext context, WpsToken input){
            WpsToken entity = (input == null ? (this.Id != 0 ? WpsToken.FromId(context, this.Id) : new WpsToken(context)) : input);
            
            entity.OwnerId = this.UserId;
            entity.GroupId = this.GroupId;
            entity.ServiceId = this.ServiceId;

            if (this.UserId == 0 && this.GroupId == 0){
                if(!string.IsNullOrEmpty(this.Username)){
                    var user = User.FromUsername(context, this.Username);
                    entity.OwnerId = user.Id;
                }
                if(!string.IsNullOrEmpty(this.Groupname)){
                    var group = Group.FromIdentifier(context, this.Groupname);
                    entity.GroupId = group.Id;
                }
            }
            if(this.ServiceId == 0 && !string.IsNullOrEmpty(this.ServiceIdentifier)){
                var service = Service.FromIdentifier(context, this.ServiceIdentifier);
                entity.ServiceId = service.Id;
            }
            
            entity.NbInputs = this.NbInputs;
            entity.NbMax = this.NbMax;            
            entity.EndTime = DateTime.Parse(this.EndTime);
            return entity;
        }

    }
}

