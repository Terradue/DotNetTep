using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/apps", "GET", Summary = "GET a list of thematic apps", Notes = "")]
    public class ThematicAppGetRequestTep : IReturn<List<WebThematicAppTep>>{
        [ApiMember(Name = "services", Description = "add services in the response", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool services { get; set; }
    }

    [Route("/apps/description", "GET", Summary = "GET OSDD of thematic apps", Notes = "")]
    public class ThematicAppDescriptionRequestTep : IReturn<List<HttpResult>>{
    }

	[Route("/apps/cache", "GET", Summary = "cache thematic apps", Notes = "")]
    public class ThematicAppCacheRequestTep : IReturn<List<HttpResult>> {
		[ApiMember(Name = "uid", Description = "identifier of the app", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Uid { get; set; }

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

    public class WebThematicAppTep {

        [ApiMember(Name = "id", Description = "id of the app", ParameterType = "query", DataType = "string", IsRequired = true)]
        public int Id { get; set; }

        [ApiMember(Name = "identifier", Description = "identifier of the app", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }

        [ApiMember(Name = "title", Description = "title of the app", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Title { get; set; }

        [ApiMember(Name = "summary", Description = "Summary of the app", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Summary { get; set; }

        [ApiMember(Name = "icon", Description = "icon url of the app", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Icon { get; set; }

        [ApiMember(Name = "selfUrl", Description = "self search url of the app", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string SelfUrl { get; set; }
        [ApiMember(Name = "accessUrl", Description = "self search url of the app", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string AccessUrl { get; set; }

        [ApiMember(Name = "Domain", Description = "Domain of the app", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Domain { get; set; }

        [ApiMember(Name = "Domains", Description = "Domain of the app (if multiple)", ParameterType = "query", DataType = "List<string>", IsRequired = true)]
        public List<string> Domains { get; set; }

        [ApiMember(Name = "HasServices", Description = "does the app have services", ParameterType = "query", DataType = "bool", IsRequired = true)]
        public bool HasServices { get; set; }

        [ApiMember(Name = "wpsServiceDomain", Description = "wpsServiceDomain of the app", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string WpsServiceDomain { get; set; }

        [ApiMember(Name = "wpsServiceTags", Description = "wpsServiceTags of the app", ParameterType = "query", DataType = "List<string>", IsRequired = true)]
        public List<string> WpsServiceTags { get; set; }

        [ApiMember(Name = "Services", Description = "Services of the app", ParameterType = "query", DataType = "List<WpsServiceOverview>", IsRequired = true)]
        public List<WpsServiceOverview> Services { get; set; }

        public WebThematicAppTep() : base() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebThematicAppTep"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebThematicAppTep(ThematicApplicationCached app, IfyContext context) {

            Id = app.Id;

            if (app.Domain != null) Domain = app.Domain.Identifier;

            var feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(app.TextFeed);
            if (feed != null) {
                var entry = feed.Items.First();
                if (entry.Title != null) Title = entry.Title.Text;
                if (entry.Summary != null) Summary = entry.Summary.Text;
                var identifiers = entry.ElementExtensions.ReadElementExtensions<string>("identifier", OwcNamespaces.Dc);
                if (identifiers.Count() > 0) this.Identifier = identifiers.First();

                var icon = entry.Links.FirstOrDefault(l => l.RelationshipType == "icon");
                if (icon != null) this.Icon = icon.Uri.AbsoluteUri;

                SelfUrl = context.BaseUrl + "/apps/search?cache=true&uid=" + this.Identifier;                

                HasServices = false;

                foreach (var offering in entry.Offerings) {
                    switch (offering.Code) {
                        case "http://www.opengis.net/spec/owc/1.0/req/atom/wps":
                            if (offering.Operations != null && offering.Operations.Length > 0) {
                                foreach (var operation in offering.Operations) {
                                    var href = operation.Href;
                                    switch (operation.Code) {
                                        case "ListProcess":
                                            HasServices = true;
                                            var uri = new Uri(href);
                                            var nvc = HttpUtility.ParseQueryString(uri.Query);
                                            foreach (var key in nvc.AllKeys) {
                                                switch (key) {
                                                    case "domain":
                                                        if(nvc[key] != null) {
                                                            if (nvc[key].Contains("${USERNAME}")) {
                                                                var user = UserTep.FromId(context, context.UserId);
                                                                user.LoadCloudUsername();
                                                                this.WpsServiceDomain = nvc[key].Replace("${USERNAME}", user.TerradueCloudUsername);
                                                            } else this.WpsServiceDomain = nvc[key];
                                                        }
                                                        break;
                                                    case "tag":
                                                        if (!string.IsNullOrEmpty(nvc[key]))
                                                            this.WpsServiceTags = nvc[key].Split(",".ToCharArray()).ToList();
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            
        }
        
    }
}

