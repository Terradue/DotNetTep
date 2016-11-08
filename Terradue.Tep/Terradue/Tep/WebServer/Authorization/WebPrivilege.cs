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

    //[Route("/priv/{id}", "GET", Summary = "GET the privilege", Notes = "Privilege is found from id")]
    //public class PrivilegeGetRequest : IReturn<WebPrivilege> {
    //    [ApiMember(Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
    //    public int Id { get; set; }
    //}

    [Route ("/priv", "GET", Summary = "GET the privileges", Notes = "")]
    public class PrivilegesGetRequest : IReturn<List<WebPrivilege>> {}

    //[Route ("/priv", "POST", Summary = "POST the privilege", Notes = "")]
    //public class PrivilegeCreateRequest : WebPrivilege, IReturn<WebPrivilege> { }

    //[Route ("/priv", "PUT", Summary = "PUT the privilege", Notes = "")]
    //public class PrivilegeUpdateRequest : WebPrivilege, IReturn<WebPrivilege> { }

    //[Route ("/priv/{id}", "DELETE", Summary = "DELETE the privilege", Notes = "Privilege is found from id")]
    //public class PrivilegeDeleteRequest : IReturn<WebPrivilege>
    //{
    //    [ApiMember (Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
    //    public int Id { get; set; }
    //}



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// User.
    /// </summary>
    public class WebPrivilege : WebEntity{

        [ApiMember(Name = "operation", Description = "Privilege operation", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Operation { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WebServer.WebPrivilege"/> class.
        /// </summary>
        public WebPrivilege() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WebServer.WebPrivilege"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebPrivilege(Privilege entity) : base(entity) {
            this.Operation = entity.OperationChar;
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
		public Privilege ToEntity(IfyContext context, Privilege input) {
			Privilege privilege = input ?? new Privilege (context);
            privilege.Identifier = this.Identifier;
            privilege.Name = this.Name;
            privilege.OperationChar = Operation;

            return privilege;
        }
            
    }
}

