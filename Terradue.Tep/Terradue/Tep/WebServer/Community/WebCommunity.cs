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

    [Route ("/job/wps/{id}/community/{cid}", "PUT", Summary = "PUT the wpsjob to the community", Notes = "")]
    public class CommunityAddWpsJobRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "id", Description = "Id of the wps job", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember (Name = "cid", Description = "Id of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int CId { get; set; }
    }

    [Route ("/data/package/{id}/community/{cid}", "PUT", Summary = "PUT the data package to the community", Notes = "")]
    public class CommunityAddDataPackageRequestTep : IReturn<WebResponseBool>
    {
        [ApiMember (Name = "id", Description = "Id of the data package", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember (Name = "cid", Description = "Id of the community", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int CId { get; set; }
    }

    [Route ("/community/search", "GET", Summary = "GET community as opensearch", Notes = "")]
    public class CommunitySearchRequestTep : IReturn<HttpResult> { }

    [Route("/community/description", "GET", Summary = "GET community as opensearch description", Notes = "")]
    public class CommunityDescriptionRequestTep : IReturn<HttpResult> { }

    public class WebCommunityTep : WebDomain {
        [ApiMember(Name="Apps", Description = "Thematic Apps link", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Apps { get; set; }

        [ApiMember(Name = "DiscussCategory", Description = "Discuss category", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string DiscussCategory { get; set; }

        public WebCommunityTep() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebWpsJob"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebCommunityTep(ThematicCommunity entity, IfyContext context = null) : base(entity) {
            Apps = entity.AppsLink;
            DiscussCategory = entity.DiscussCategory;
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
            return entity;
        }

    }
}
