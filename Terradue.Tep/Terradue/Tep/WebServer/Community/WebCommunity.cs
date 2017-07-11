using System;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep
{

    [Route ("/community/user", "POST", Summary = "POST the user into the community", Notes = "")]
    public class CommunityAddUserRequestTep : IReturn<WebResponseBool> {
        [ApiMember (Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
        [ApiMember (Name = "username", Description = "Username of the user (current user if null)", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Username { get; set; }
        [ApiMember (Name = "role", Description = "Role of the user", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Role { get; set; }
    }

    [Route ("/community/user", "PUT", Summary = "PUT the user into the community", Notes = "")]
    public class CommunityUpdateUserRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
        [ApiMember (Name = "username", Description = "Username of the user (current user if null)", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Username { get; set; }
        [ApiMember (Name = "role", Description = "Role of the user", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Role { get; set; }
    }

    [Route ("/community", "PUT", Summary = "PUT the the community", Notes = "")]
    public class CommunityUpdateRequestTep : WebCommunityTep, IReturn<WebResponseBool> {}

    [Route ("/community/user", "DELETE", Summary = "POST the current user into the community", Notes = "")]
    public class CommunityRemoveUserRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
        [ApiMember (Name = "username", Description = "Username of the user (current user if null)", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Username { get; set; }
    }

    [Route ("/community/{identifier}", "DELETE", Summary = "DELETE the current community", Notes = "")]
    public class CommunityDeleteRequest : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route ("/community/search", "GET", Summary = "GET community as opensearch", Notes = "")]
    public class CommunitySearchRequestTep : IReturn<HttpResult> {
		[ApiMember(Name = "apikey", Description = "Api key of the user", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string ApiKey { get; set; }
    }

    [Route("/community/description", "GET", Summary = "GET community as opensearch description", Notes = "")]
    public class CommunityDescriptionRequestTep : IReturn<HttpResult> { }

    [Route("/community/{identifier}/collection/{collIdentifier}", "POST", Summary = "POST the collection into the community", Notes = "")]
	public class CommunityAddCollectionRequestTep : IReturn<WebResponseBool> {
		[ApiMember(Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Identifier { get; set; }
		[ApiMember(Name = "collIdentifier", Description = "Identifier of the collection", ParameterType = "query", DataType = "string", IsRequired = false)]
		public string CollIdentifier { get; set; }
	}

	[Route("/community/{identifier}/collection/{collIdentifier}", "DELETE", Summary = "DELETE the collection from the community", Notes = "")]
	public class CommunityRemoveCollectionRequestTep : IReturn<WebResponseBool> {
		[ApiMember(Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Identifier { get; set; }
		[ApiMember(Name = "collIdentifier", Description = "Identifier of the collection", ParameterType = "query", DataType = "string", IsRequired = false)]
		public string CollIdentifier { get; set; }
	}

	[Route("/community/{identifier}/service/wps/{wpsIdentifier}", "POST", Summary = "POST the wps service into the community", Notes = "")]
	public class CommunityAddWpsServiceRequestTep : IReturn<WebResponseBool> {
		[ApiMember(Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Identifier { get; set; }
		[ApiMember(Name = "wpsIdentifier", Description = "Identifier of the wps service", ParameterType = "query", DataType = "string", IsRequired = false)]
		public string WpsIdentifier { get; set; }
	}

	[Route("/community/{identifier}/service/wps/{wpsIdentifier}", "DELETE", Summary = "DELETE the wps service from the community", Notes = "")]
	public class CommunityRemoveWpsServiceRequestTep : IReturn<WebResponseBool> {
		[ApiMember(Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Identifier { get; set; }
		[ApiMember(Name = "wpsIdentifier", Description = "Identifier of the wps service", ParameterType = "query", DataType = "string", IsRequired = false)]
		public string WpsIdentifier { get; set; }
	}

    public class WebCommunityTep : WebDomain {
        [ApiMember(Name="Apps", Description = "Thematic Apps link", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Apps { get; set; }

        [ApiMember(Name = "DiscussCategory", Description = "Discuss category", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string DiscussCategory { get; set; }

        [ApiMember(Name = "DefaultRole", Description = "Default role", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string DefaultRole { get; set; }

        public WebCommunityTep() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebWpsJob"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebCommunityTep(ThematicCommunity entity, IfyContext context = null) : base(entity) {
            Apps = entity.AppsLink;
            DiscussCategory = entity.DiscussCategory;
            Name = entity.Name ?? entity.Identifier;
            DefaultRole = entity.DefaultRoleName;
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
        /// <param name="input">Input.</param>
        public ThematicCommunity ToEntity(IfyContext context, ThematicCommunity input){
            ThematicCommunity entity = (input == null ? new ThematicCommunity(context) : input);

            entity.DiscussCategory = DiscussCategory;
            entity.AppsLink = Apps;
            entity.IconUrl = IconeUrl;
            entity.Identifier = TepUtility.ValidateIdentifier(Identifier);
            entity.Name = Name;
            entity.Description = Description;
            entity.DefaultRoleName = DefaultRole;
            if (Kind == (int)DomainKind.Public || Kind == (int)DomainKind.Private) entity.Kind = (DomainKind)Kind;
            return entity;
        }

    }
}
