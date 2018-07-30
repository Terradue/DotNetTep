using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/apps", "GET", Summary = "GET a list of thematic apps", Notes = "")]
    public class ThematicAppGetRequestTep : IReturn<List<WebThematicAppTep>>{}

    [Route("/apps/description", "GET", Summary = "GET OSDD of thematic apps", Notes = "")]
    public class ThematicAppDescriptionRequestTep : IReturn<List<HttpResult>>{
    }

	[Route("/apps/cache", "GET", Summary = "cache thematic apps", Notes = "")]
    public class ThematicAppCacheRequestTep : IReturn<List<HttpResult>> {
		[ApiMember(Name = "username", Description = "identifier of the user", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Username { get; set; }

		[ApiMember(Name = "community", Description = "identifier of the community", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Community { get; set; }
	}
    
    [Route ("/apps/search", "GET", Summary = "search for thematic apps", Notes = "")]
    public class ThematicAppSearchRequestTep : IReturn<List<HttpResult>> {
		[ApiMember(Name = "cache", Description = "uses cached apps", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool cache { get; set; }
	}

	[Route("/apps/available", "GET", Summary = "Check if apps is available", Notes = "")]
	public class ThematicAppCheckAvailabilityRequest : IReturn<WebResponseBool> { 
		[ApiMember(Name = "uid", Description = "identifier of the app", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Uid { get; set; }

        [ApiMember(Name = "index", Description = "index of the app", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Index { get; set; }
	}

    [Route("/user/current/apps/search", "GET", Summary = "search for thematic apps", Notes = "")]
    public class ThematicAppCurrentUserSearchRequestTep : IReturn<List<HttpResult>> { 
		[ApiMember(Name = "cache", Description = "uses cached apps", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool cache { get; set; }
	}

    [Route("/community/{domain}/apps/search", "GET", Summary = "search for thematic apps", Notes = "")]
    public class ThematicAppByCommunitySearchRequestTep : IReturn<List<HttpResult>>{
        [ApiMember (Name = "domain", Description = "identifier of the domain", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Domain { get; set; }

		[ApiMember(Name = "cache", Description = "uses cached apps", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool cache { get; set; }
    }

    [Route("/community/{domain}/apps/", "POST", Summary = "search for thematic apps", Notes = "")]
    public class ThematicAppAddToCommunityRequestTep : IReturn<List<HttpResult>> {
        [ApiMember(Name = "domain", Description = "identifier of the domain", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Domain { get; set; }

        [ApiMember(Name = "appUrl", Description = "url of the app", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string AppUrl { get; set; }
    }

    //[Route("/apps", "POST", Summary = "create thematic App", Notes = "")]
    //public class ThematicAppCreateRequestTep : IReturn<WebThematicAppTep> {
    //    [ApiMember(Name = "identifier", Description = "thematic app identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
    //    public string Identifier { get; set; }

    //    [ApiMember(Name = "title", Description = "thematic app title", ParameterType = "query", DataType = "string", IsRequired = true)]
    //    public string Title { get; set; }

    //    [ApiMember(Name = "description", Description = "thematic app description", ParameterType = "query", DataType = "string", IsRequired = true)]
    //    public string Description { get; set; }

    //    [ApiMember(Name = "icon", Description = "thematic app icon url", ParameterType = "query", DataType = "string", IsRequired = true)]
    //    public string Icon { get; set; }

    //    [ApiMember(Name = "index", Description = "catalog index", ParameterType = "query", DataType = "string", IsRequired = true)]
    //    public string Index { get; set; }

    //    [ApiMember(Name = "url", Description = "external url of the thematic app", ParameterType = "query", DataType = "string", IsRequired = true)]
    //    public string Url { get; set; }
    //}

    public class WebThematicAppTep : WebDataPackageTep {
        
        public WebThematicAppTep() : base() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebThematicAppTep"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebThematicAppTep(ThematicApplication entity) : base(entity) {
        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
        /// <param name="input">Input.</param>
        public ThematicApplication ToEntity(IfyContext context, ThematicApplication input){
            ThematicApplication entity = (ThematicApplication)base.ToEntity (context, input);

            return entity;
        }

    }
}

