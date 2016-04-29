using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {

    [Route("/share", "POST", Summary = "share an entity", Notes = "")]
    public class ShareCreateRequestTep : IReturn<WebResponseBool>{
        [ApiMember(Name="url", Description = "url representing the item shared", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string url { get; set; }

        [ApiMember(Name="id", Description = "thematic applicaiton id", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string id { get; set; }

        [ApiMember(Name="visibility", Description = "type of sharing", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string visibility { get; set; }

        [ApiMember(Name="groups", Description = "group identifier", ParameterType = "query", DataType = "int", IsRequired = true)]
        public List<int> groups { get; set; }
    }

    [Route("/share", "GET", Summary = "share an entity", Notes = "")]
    public class ShareGetRequestTep {
        [ApiMember(Name="url", Description = "url representing the item shared", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string url { get; set; }

        [ApiMember(Name="id", Description = "thematic applicaiton id", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string id { get; set; }
    }

     [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure   | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ShareServiceTep : ServiceStack.ServiceInterface.Service {

        public object Post(ShareCreateRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            object result;
            context.Open();

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            UrlBasedOpenSearchable urlToShare = new UrlBasedOpenSearchable(context, new OpenSearchUrl(request.url), ose);
            IOpenSearchResultCollection searchResult = null;
            try{
                searchResult = ose.Query(urlToShare, new System.Collections.Specialized.NameValueCollection());
            }catch(Exception e){
                throw e;
            }

            foreach (IOpenSearchResultItem item in searchResult.Items) {
                if (item is AtomItem && ((AtomItem)item).ReferenceData is Entity) {
                    Entity ent = ((AtomItem)item).ReferenceData as Entity;
                    if (ent.OwnerId == context.UserId) {
                        string visibility = (request.visibility != null ? request.visibility : "public");
                        switch (visibility){
                            case "public":
                                ent.StoreGlobalPrivileges();
                                break;
                            case "restricted":
                                ent.RemoveGlobalPrivileges();
                                if(request.groups != null && request.groups.Count > 0)
                                    ent.StorePrivilegesForGroups(request.groups.ToArray(), true);
                                break;
                            case "private":
                                ent.StorePrivilegesForGroups(null, true);
								ent.RemoveGlobalPrivileges();
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            context.Close ();
            return new WebResponseBool(true);
        }

        public object Get(ShareGetRequestTep request) {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            context.Open();

            var redirect = new UriBuilder(context.BaseUrl);
            redirect.Path = "geobrowser";
            string redirectUrl = redirect.Uri.AbsoluteUri + "/#!";

            Match match = Regex.Match(new Uri(request.url).LocalPath.Replace(new Uri(context.BaseUrl).LocalPath, ""), @"(/.*)(/?.*)/search");
            if (match.Success) {
                var resultType = match.Groups[1].Value.Trim('/');
                if (resultType.Equals(EntityType.GetEntityType(typeof(Series)).Keyword)) {
                    redirectUrl += "resultType=" + EntityType.GetEntityType(typeof(Series)).Keyword;
                } else if (resultType.Equals(EntityType.GetEntityType(typeof(DataPackage)).Keyword)) {
                    redirectUrl += "resultType=" + EntityType.GetEntityType(typeof(DataPackage)).Keyword;
                } else if (resultType.Contains(EntityType.GetEntityType(typeof(DataPackage)).Keyword)){
                    redirectUrl += "resultType=" + "data";//in this case it is a search (over a data package) so we use data keyword
                } else if (resultType.Equals(EntityType.GetEntityType(typeof(WpsJob)).Keyword)){
                    redirectUrl += "resultType=" + EntityType.GetEntityType(typeof(WpsJob)).Keyword;
                } else if (resultType.Equals(EntityType.GetEntityType(typeof(WpsProvider)).Keyword)){
                    redirectUrl += "resultType=" + EntityType.GetEntityType(typeof(WpsProvider)).Keyword;
                } else if (resultType.Equals(EntityType.GetEntityType(typeof(WpsProcessOffering)).Keyword)){
                    redirectUrl += "resultType=" + EntityType.GetEntityType(typeof(WpsProcessOffering)).Keyword;
                } else {
                    if (request.url.StartsWith("https://data.terradue.com") || request.url.StartsWith("https://data2.terradue.com")) {
                        redirectUrl += "resultType=" + "data";
                    } else {
                        redirectUrl += "resultType=" + "na";
                    }
                }
                redirectUrl += "&url=" + HttpUtility.UrlEncode(request.url);
            }
            else
                throw new Exception("Wrong format shared url");

            var keyword = match.Groups[1].Value.StartsWith("/") ? match.Groups[1].Value.Substring(1) : match.Groups[1].Value;
            EntityType entityType = EntityType.GetEntityTypeFromKeyword(keyword);

            context.Close ();

            return new HttpResult(){ StatusCode = System.Net.HttpStatusCode.Redirect, Headers = {{HttpHeaders.Location, redirectUrl}}};
        }
    }

}