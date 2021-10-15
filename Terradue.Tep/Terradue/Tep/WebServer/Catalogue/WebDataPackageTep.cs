using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

    [Route("/data/package/default", "PUT", Summary = "PUT a datapackage", Notes = "Update a datapackage in database")]
    public class DataPackageUpdateDefaultRequestTep : WebDataPackage, IReturn<WebDataPackage>{}

    [Route("/data/package/default/item", "POST", Summary = "POST item to default datapackage", Notes = "datapackage item is contained in the body")]
    public class DataPackageAddItemToDefaultRequestTep : WebDataPackageItem, IReturn<WebDataPackage>
    {
    }

    [Route("/data/package/default/search", "GET", Summary = "GET default datapackage search", Notes = "datapackage item is contained in the body")]
    public class DataPackageSearchDefaultRequestTep : IReturn<HttpResult>
    {
    }

	//[Route("/data/package/_index/search", "GET", Summary = "GET my index datapackage search", Notes = "datapackage item is contained in the body")]
	//public class DataPackageSearchMyIndexRequestTep : IReturn<HttpResult> {
	//}

	//[Route("/data/package/_products/search", "GET", Summary = "GET my products datapackage search", Notes = "datapackage item is contained in the body")]
	//public class DataPackageSearchMyProductsRequestTep : IReturn<HttpResult> {
	//}

    [Route("/data/package/default/description", "GET", Summary = "GET default datapackage description", Notes = "datapackage item is contained in the body")]
    public class DataPackageDescriptionDefaultRequestTep : IReturn<HttpResult>
    {
    }

    [Route("/data/package/{Identifier}", "GET", Summary = "GET datapackage", Notes = "datapackage is contained in the body")]
    public class GetDataPackageTep : IReturn<WebDataPackage> {
        [ApiMember(Name = "Identifier", Description = "DELETE datapackage", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/data/package/default/items", "POST", Summary = "POST item to default datapackage", Notes = "datapackage item is contained in the body")]
    public class DataPackageAddItemsToDefaultRequestTep : List<WebDataPackageItem>, IReturn<WebDataPackage>
    {
    }

    [Route("/data/package/default", "POST", Summary = "POST item to default datapackage", Notes = "datapackage item is contained in the body")]
    public class DataPackageSaveDefaultRequestTep : WebDataPackage, IReturn<WebDataPackage>
    {
        [ApiMember(Name = "Overwrite", Description = "indicates if we overwrite the dp", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool Overwrite { get; set; }
    }

    [Route("/data/package/default", "DELETE", Summary = "DELETE item to default datapackage of current user", Notes = "datapackage item is contained in the body")]
    public class DataPackageClearCurrentDefaultRequestTep : IReturn<WebDataPackage>
    {
    }

    [Route("/data/package/default/{userId}", "DELETE", Summary = "DELETE item to default datapackage", Notes = "datapackage item is contained in the body")]
    public class DataPackageClearDefaultRequestTep : IReturn<WebDataPackage>
    {
        [ApiMember(Name = "userId", Description = "user id", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string userId { get; set; }
    }

    [Route("/data/package/default/item", "DELETE", Summary = "DELETE item to default datapackage", Notes = "datapackage item is contained in the body")]
    public class DataPackageRemoveItemFromDefaultRequestTep : IReturn<WebDataPackage>
    {
        [ApiMember(Name = "url", Description = "item url", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Url { get; set; }
    }

    [Route("/data/package/{Identifier}", "DELETE", Summary = "DELETE datapackage", Notes = "datapackage is contained in the body")]
    public class DataPackageDeleteRequestTep : IReturn<WebDataPackage>{
        [ApiMember(Name="Identifier", Description = "DELETE datapackage", ParameterType = "path", DataType = "string", IsRequired = true)]
        public string Identifier { get; set; }
    }

    [Route("/data/package", "POST", Summary = "POST a datapackage", Notes = "Add a new datapackage in database")]
    public class DataPackageCreateRequestTep : WebDataPackageTep, IReturn<WebDataPackageTep>
    {
        [ApiMember(Name = "Overwrite", Description = "indicates if we overwrite the dp", ParameterType = "query", DataType = "bool", IsRequired = false)]
        public bool Overwrite { get; set; }
    }

    [Route("/data/package", "PUT", Summary = "PUT a datapackage", Notes = "Update a datapackage in database")]
    public class DataPackageUpdateRequestTep : WebDataPackageTep, IReturn<WebDataPackageTep>{
		[ApiMember(Name = "access", Description = "Define if the data package shall be public or private", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Access { get; set; }
	}

    [Route("/data/package/export", "PUT", Summary = "PUT a datapackage", Notes = "export a datapackage as series")]
    public class DataPackageExportRequestTep : WebDataPackageTep, IReturn<WebSeries>{}

    [Route("/data/package/{dpId}/group", "GET", Summary = "GET list of groups that can access a datapackage", Notes = "")]
    public class DataPackageGetGroupsRequestTep : IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "dpId", Description = "identifier of the datapackage", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string DpId { get; set; }
    }

    [Route("/data/package/{dpId}/group", "POST", Summary = "POST group to datapackage", Notes = "")]
    public class DataPackageAddGroupRequestTep : WebGroup, IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "dpId", Description = "identifier of the datapackage", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string DpId { get; set; }
    }

    [Route("/data/package/{dpId}/group", "PUT", Summary = "PUT group to datapackage", Notes = "")]
    public class DataPackageUpdateGroupsRequestTep : List<int>, IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "dpId", Description = "identifier of the datapackage", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string DpId { get; set; }
    }

    [Route("/data/package/{dpId}/group/{Id}", "DELETE", Summary = "DELETE group to datapackage", Notes = "")]
    public class DataPackageDeleteGroupRequestTep : IReturn<List<WebGroup>>
    {
        [ApiMember(Name = "dpId", Description = "identifier of the datapackage", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string DpId { get; set; }

        [ApiMember(Name = "Id", Description = "id of the group", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int Id { get; set; }
    }

    [Route("/data/package/{dpId}/available", "GET", Summary = "GET list of groups that can access a datapackage", Notes = "")]
    public class DataPackageGetAvailableIdentifierRequestTep : IReturn<WebResponseBool> {
		[ApiMember(Name = "dpId", Description = "identifier of the datapackage", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string DpId { get; set; }
	}

    public class WebDataPackageTep : Terradue.WebService.Model.WebDataPackage {

        [ApiMember(Name="AccessKey", Description = "Remote resource AccessKey", ParameterType = "path", DataType = "string", IsRequired = false)]
        public string AccessKey { get; set; }

        [ApiMember(Name="IsPublic", Description = "Remote resource IsPublic", ParameterType = "path", DataType = "bool", IsRequired = false)]
        public bool IsPublic { get; set; }

        [ApiMember(Name="NbItems", Description = "Remote resource NbItems", ParameterType = "path", DataType = "int", IsRequired = false)]
        public int NbItems { get; set; }

        [ApiMember(Name="Username", Description = "Name of the owner of the remote resource", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Username { get; set; }

        public WebDataPackageTep() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebDataPackageTep"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebDataPackageTep(DataPackage entity, IfyContext context = null) : base(entity)
        {
            this.AccessKey = entity.AccessKey;
            this.IsPublic = entity.IsPublic();
            this.Items = new List<WebDataPackageItem>();
            foreach (RemoteResource item in entity.Resources) this.Items.Add(new WebDataPackageItem(item));
            this.NbItems = this.Items.Count;

            if (context != null) {
                try {
                    this.Username = User.FromId(context, entity.OwnerId).Username;
                } catch (Exception) {
                }
            }
        }

        /// <summary>
        /// To the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
        public DataPackage ToEntity(IfyContext context, DataPackage input){
            DataPackage result = (input == null ? new DataPackage(context) : input);

            result.Name = this.Name;
            result.Identifier = this.Identifier;
            if (!string.IsNullOrEmpty (this.DomainId)) result.DomainId = Int32.Parse (this.DomainId);
            result.Kind = this.Kind;
            result.Items = new EntityList<RemoteResource>(context);
            result.Items.Template.ResourceSet = result;
            if (this.Items != null) {
                foreach (WebDataPackageItem item in this.Items) {
                    RemoteResource res = (item.Id == 0) ? new RemoteResource(context) : RemoteResource.FromId(context, item.Id);
                    res = item.ToEntity(context, res);
                    result.Items.Include(res);
                }
            }

            return result;
        }
    }
}


