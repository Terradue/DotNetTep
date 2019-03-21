using System;
using System.Collections.Generic;
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

    [Route("/community", "POST", Summary = "PUT the the community", Notes = "")]
    public class CommunityCreateRequestTep : WebCommunityTep, IReturn<WebResponseBool> { }

    [Route ("/community/user", "DELETE", Summary = "POST the current user into the community", Notes = "")]
    public class CommunityRemoveUserRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
        [ApiMember (Name = "username", Description = "Username of the user (current user if null)", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Username { get; set; }
        [ApiMember(Name = "reason", Description = "Reason why the user is removed", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Reason { get; set; }
    }

    [Route ("/community/{identifier}", "DELETE", Summary = "DELETE the current community", Notes = "")]
    public class CommunityDeleteRequest : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "identifier", Description = "Identifier of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route ("/community/search", "GET", Summary = "GET community as opensearch", Notes = "")]
    public class CommunitySearchRequestTep : IReturn<HttpResult> {
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
        [ApiMember(Name="Apps", Description = "Thematic Apps link", ParameterType = "query", DataType = "List<string>", IsRequired = true)]
        public List<string> Apps { get; set; }

        [ApiMember(Name = "DiscussCategory", Description = "Discuss category", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string DiscussCategory { get; set; }

        [ApiMember(Name = "DefaultRole", Description = "Default role", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string DefaultRole { get; set; }

        [ApiMember(Name = "EmailNotification", Description = "Email Notification requested for user requesting access", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool EmailNotification { get; set; }

        [ApiMember(Name = "EnableJoinRequest", Description = "Enable user join request", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool EnableJoinRequest { get; set; }

        [ApiMember(Name = "Contributor", Description = "Contributor name", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Contributor { get; set; }

        [ApiMember(Name = "ContributorIcon", Description = "Contributor icon", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string ContributorIcon { get; set; }

        [ApiMember(Name = "Links", Description = "Domain Links", ParameterType = "query", DataType = "List<WebDataPackageItem>", IsRequired = false)]
        public List<WebDataPackageItem> Links { get; set; }

        public WebCommunityTep() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebWpsJob"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebCommunityTep(ThematicCommunity entity, IfyContext context = null) : base(entity) {
            Apps = entity.AppsLinks;
            DiscussCategory = entity.DiscussCategory;
            Name = entity.Name ?? entity.Identifier;
            DefaultRole = entity.DefaultRoleName;
            EmailNotification = entity.EmailNotification;
            EnableJoinRequest = entity.EnableJoinRequest;
            Contributor = entity.Contributor;
            ContributorIcon = entity.ContributorIcon;
            var domainLinks = entity.GetDomainLinks();
            domainLinks.LoadItems();
            if (domainLinks.Items != null && domainLinks.Items.Count > 0) {
                Links = new List<WebDataPackageItem>();
                foreach (RemoteResource item in domainLinks.Items) Links.Add(new WebDataPackageItem(item));
            }
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
            entity.AppsLinks = Apps;
            entity.IconUrl = IconeUrl;
            entity.Identifier = TepUtility.ValidateIdentifier(Identifier);
            entity.Name = Name;
            entity.Description = Description;
            entity.EmailNotification = EmailNotification;
            entity.EnableJoinRequest = EnableJoinRequest;
            entity.DefaultRoleName = DefaultRole;
            entity.Contributor = Contributor;
            entity.ContributorIcon = ContributorIcon;
            if (Kind == (int)DomainKind.Public || Kind == (int)DomainKind.Private || Kind == (int)DomainKind.Hidden) entity.Kind = (DomainKind)Kind;
            if (Links != null && Links.Count > 0) {
                foreach (WebDataPackageItem item in Links) {
                    RemoteResource res = (item.Id == 0) ? new RemoteResource(context) : RemoteResource.FromId(context, item.Id);
                    res = item.ToEntity(context, res);
                    entity.Links.Add(res);
                }
            }
            return entity;
        }

    }
}
