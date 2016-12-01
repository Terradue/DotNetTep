using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
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

    [Route ("/role/grant", "GET", Summary = "GET the roles", Notes = "")]
    public class RolesGrantGetRequest : IReturn<List<WebRoleGrant>> {
        [ApiMember (Name = "domainId", Description = "domain id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int DomainId { get; set; }

        [ApiMember (Name = "userId", Description = "user id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int UserId { get; set; }
    }

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

    [Route ("/role/grant", "POST", Summary = "GRANT the role", Notes = "Role is found from id")]
    public class RoleGrantRequest : IReturn<WebRole> {
        [ApiMember (Name = "roleId", Description = "Role id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int RoleId { get; set; }

        [ApiMember (Name = "userId", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int UserId { get; set; }

        [ApiMember (Name = "groupId", Description = "Group id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int GroupId { get; set; }

        [ApiMember (Name = "domainId", Description = "Domain id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int DomainId { get; set; }
    }

    [Route ("/role/grant", "DELETE", Summary = "GRANT the role", Notes = "Role is found from id")]
    public class RoleGrantDeleteRequest : IReturn<WebRole>
    {
        [ApiMember (Name = "roleId", Description = "Role id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int RoleId { get; set; }

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

    public class WebRoleGrant { 
        [ApiMember (Name = "role", Description = "Role ", ParameterType = "query", DataType = "WebRole", IsRequired = true)]
        public WebRole Role { get; set; }

        [ApiMember (Name = "domain", Description = "Domain ", ParameterType = "query", DataType = "WebDomain", IsRequired = true)]
        public WebDomain Domain { get; set; }

        [ApiMember (Name = "user", Description = "User ", ParameterType = "query", DataType = "WebUser", IsRequired = false)]
        public WebUser User { get; set; }

        [ApiMember (Name = "group", Description = "Group ", ParameterType = "query", DataType = "WebGroup", IsRequired = false)]
        public WebGroup Group { get; set; }
    }

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
        public WebRole(Role entity, bool loadPriv = false) : base(entity) {
            this.Description = entity.Description;
            if (loadPriv) {
                Privileges = new List<int> ();
                foreach (var priv in entity.GetPrivileges ()) this.Privileges.Add (priv.Id);
            }
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

