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

    [Route("/domain/{id}", "GET", Summary = "GET the domain", Notes = "Domain is found from id")]
    public class DomainGetRequest : IReturn<WebDomain> {
        [ApiMember(Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route ("/domain", "GET", Summary = "GET the domains", Notes = "")]
    public class DomainsGetRequest : IReturn<List<WebDomain>> {
        [ApiMember (Name = "kind", Description = "kind of domain", ParameterType = "query", DataType = "int", IsRequired = false)]
        public int Kind { get; set; }
    }

    [Route ("/domain", "POST", Summary = "POST the domain", Notes = "")]
    public class DomainCreateRequest : WebDomain, IReturn<WebDomain> { }

    [Route ("/domain", "PUT", Summary = "PUT the domain", Notes = "")]
    public class DomainUpdateRequest : WebDomain, IReturn<WebDomain> { }

    [Route ("/domain/{id}", "DELETE", Summary = "DELETE the domain", Notes = "Domain is found from id")]
    public class DomainDeleteRequest : IReturn<WebDomain>
    {
        [ApiMember (Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// User.
    /// </summary>
    public class WebDomain : WebEntity{

        [ApiMember(Name = "description", Description = "Domain description", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Description { get; set; }

        [ApiMember (Name = "kind", Description = "Domain type", ParameterType = "query", DataType = "int", IsRequired = false)]
        public int Kind { get; set; }

        [ApiMember (Name = "IconeUrl", Description = "icone url of domain", ParameterType = "query", DataType = "int", IsRequired = false)]
        public string IconeUrl { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WebServer.WebDomain"/> class.
        /// </summary>
        public WebDomain() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.WebServer.WebDomain"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebDomain(Domain entity) : base(entity) {
            this.Description = entity.Description;
            this.Kind = (int)entity.Kind;
            this.IconeUrl = entity.IconUrl;
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
		public Domain ToEntity(IfyContext context, Domain input) {
			Domain domain = input ?? new Domain (context);
            domain.Identifier = this.Identifier;
            domain.Description = Description;
            domain.Kind = (DomainKind)Kind;
            domain.IconUrl = IconeUrl;

            return domain;
        }
            
    }
}

