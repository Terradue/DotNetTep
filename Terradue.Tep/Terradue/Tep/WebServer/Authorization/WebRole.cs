using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using Terradue.Authentication.Umsso;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/role/{id}", "GET", Summary = "GET the role", Notes = "Role is found from id")]
    public class RoleGetRequest : IReturn<WebRole> {
        [ApiMember(Name = "id", Description = "role id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route ("/role", "GET", Summary = "GET the roles", Notes = "")]
    public class RolesGetRequest : IReturn<List<WebRole>> {}

    [Route ("/role", "POST", Summary = "POST the role", Notes = "")]
    public class RoleCreateRequest : WebRole, IReturn<WebRole> { }

    [Route ("/role", "PUT", Summary = "PUT the role", Notes = "")]
    public class RoleUpdateRequest : WebRole, IReturn<WebRole> { }

    [Route ("/role/{id}", "DELETE", Summary = "DELETE the role", Notes = "Role is found from id")]
    public class RoleDeleteRequest : IReturn<WebRole>
    {
        [ApiMember (Name = "id", Description = "Role id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route ("/role/priv", "PUT", Summary = "PUT the role", Notes = "")]
    public class RoleUpdatePrivilegesRequest : WebRole, IReturn<WebRole> { }

    [Route ("/role/{id}/grant", "PUT", Summary = "GRANT the role", Notes = "Role is found from id")]
    public class RoleGrantRequest : IReturn<WebRole>
    {
        [ApiMember (Name = "id", Description = "Role id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember (Name = "userId", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int UserId { get; set; }

        [ApiMember (Name = "groupId", Description = "Group id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int GroupId { get; set; }

        [ApiMember (Name = "domainId", Description = "Domain id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int DomainId { get; set; }
    }

    [Route ("/role/{id}/grant", "DELETE", Summary = "GRANT the role", Notes = "Role is found from id")]
    public class RoleGrantDeleteRequest : IReturn<WebRole>
    {
        [ApiMember (Name = "id", Description = "Role id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember (Name = "userId", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int UserId { get; set; }

        [ApiMember (Name = "groupId", Description = "Group id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int GroupId { get; set; }

        [ApiMember (Name = "domainId", Description = "Domain id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int DomainId { get; set; }
    }

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Role.
    /// </summary>
    public class WebRole : WebEntity{

        [ApiMember(Name = "description", Description = "Role description", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Description { get; set; }

        [ApiMember (Name = "privileges", Description = "Role privileges", ParameterType = "query", DataType = "List<int>", IsRequired = false)]
        public List<int> Privileges { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WebServer.WebRole"/> class.
        /// </summary>
        public WebRole() {
            Privileges = new List<int> ();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WebServer.WebRole"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebRole(Role entity) : base(entity) {
            this.Description = entity.Description;
            Privileges = new List<int> ();
            foreach (var p in entity.ItemPrivileges) Privileges.Add (p.Id);
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
		public Role ToEntity(IfyContext context, Role input) {
			Role role = input ?? new Role (context);
            role.Identifier = this.Identifier;
            role.Name = this.Name;
            role.Description = Description;

            return role;
        }
            
    }
}

