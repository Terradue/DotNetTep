using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep.Controller;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/data/collection/{Id}", "GET", Summary = "GET a list of series", Notes = "Series can be filtered by User Id, Status, ...")]
    public class SerieGetRequestTep : IReturn<WebDataCollectionTep>
    {
        [ApiMember(Name="Id", Description = "Series id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route("/data/collection", "PUT", Summary = "PUT update collection", Notes = "")]
    public class CollectionUpdateRequestTep : WebSeries, IReturn<List<WebGroup>> {
        
        [ApiMember(Name = "access", Description = "Define if the collection shall be public or private", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Access { get; set; }

    }

    [Route("/data/collection/{collId}/group", "GET", Summary = "GET list of groups that can access a collection", Notes = "")]
    public class CollectionGetGroupsRequestTep : IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "collId", Description = "id of the collection", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int CollId { get; set; }
    }

    [Route("/data/collection/{collId}/group", "POST", Summary = "POST group to collection", Notes = "")]
    public class CollectionAddGroupRequestTep : WebGroup, IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "collId", Description = "id of the collection", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int CollId { get; set; }
    }

    [Route("/data/collection/{collId}/group/{Id}", "DELETE", Summary = "DELETE group to collection", Notes = "")]
    public class CollectionDeleteGroupRequestTep : IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "collId", Description = "id of the collection", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int CollId { get; set; }

        [ApiMember(Name = "Id", Description = "id of the group", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    public class WebDataCollectionTep : Terradue.WebService.Model.WebSeries {

        [ApiMember(Name="IsPublic", Description = "Remote resource IsPublic", ParameterType = "path", DataType = "bool", IsRequired = false)]
        public bool IsPublic { get; set; }

        public WebDataCollectionTep() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebDataPackageTep"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebDataCollectionTep(Collection entity) : base(entity)
        {
            this.IsPublic = entity.IsPublic();
        }
    }
}


