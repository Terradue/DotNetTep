using System;
using System.Collections.Generic;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

	[Route("/app/editor", "POST", Summary = "POST a thematic app", Notes = "")]
	public class ThematicAppEditorPostRequestTep : WebThematicAppEditor { }

	[Route("/app/editor", "GET", Summary = "POST a thematic app", Notes = "")]
	public class ThematicAppEditorGetRequestTep : IReturn<WebThematicAppEditor> {
		[ApiMember(Name = "Url", Description = "Url", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Url { get; set; }
    }

	public class WebThematicAppEditor {

		[ApiMember(Name = "Identifier", Description = "Identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Identifier { get; set; }

		[ApiMember(Name = "Title", Description = "Title", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Title { get; set; }

		[ApiMember(Name = "Summary", Description = "Summary", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Summary { get; set; }

		[ApiMember(Name = "Icon", Description = "Icon", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Icon { get; set; }

		[ApiMember(Name = "Authors", Description = "Authors", ParameterType = "query", DataType = "List<WebThematicAppAuthor>", IsRequired = true)]
        public List<WebThematicAppAuthor> Authors { get; set; }

		[ApiMember(Name = "Categories", Description = "Categories", ParameterType = "query", DataType = "List<WebThematicAppCategory>", IsRequired = true)]
		public List<WebThematicAppCategory> Categories { get; set; }

		[ApiMember(Name = "MapFeatures", Description = "MapFeatures", ParameterType = "query", DataType = "List<WebThematicAppMapFeature>", IsRequired = true)]
		public List<WebThematicAppMapFeature> MapFeatures { get; set; }

		[ApiMember(Name = "DataContexts", Description = "DataContexts", ParameterType = "query", DataType = "List<WebThematicAppDataContext>", IsRequired = true)]
		public List<WebThematicAppDataContext> DataContexts { get; set; }

		[ApiMember(Name = "OpensearchTableOfferingPresent", Description = "OpensearchTableOfferingPresent", ParameterType = "query", DataType = "bool", IsRequired = true)]
		public bool OpensearchTableOfferingPresent { get; set; }

		[ApiMember(Name = "FeatureBasketOfferingPresent", Description = "FeatureBasketOfferingPresent", ParameterType = "query", DataType = "bool", IsRequired = true)]
		public bool FeatureBasketOfferingPresent { get; set; }

        [ApiMember(Name = "DataPackageOfferingPresent", Description = "DataPackageOfferingPresent", ParameterType = "query", DataType = "bool", IsRequired = true)]
        public bool DataPackageOfferingPresent { get; set; }

		[ApiMember(Name = "WpsServiceOfferingPresent", Description = "WpsServiceOfferingPresent", ParameterType = "query", DataType = "bool", IsRequired = true)]
		public bool WpsServiceOfferingPresent { get; set; }

		[ApiMember(Name = "WpsServiceOfferingDomain", Description = "WpsServiceOfferingDomain", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string WpsServiceOfferingDomain { get; set; }

		[ApiMember(Name = "WpsServiceOfferingTags", Description = "WpsServiceOfferingTags", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string WpsServiceOfferingTags { get; set; }

		[ApiMember(Name = "BaseMaps", Description = "BaseMaps", ParameterType = "query", DataType = "List<string>", IsRequired = true)]
		public List<string> BaseMaps { get; set; }

		[ApiMember(Name = "Index", Description = "Index", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Index { get; set; }

		public WebThematicAppEditor() { }

        public WebThematicAppEditor(OwsContextAtomEntry entry) {
            Identifier = entry.;
            if (entry.Title != null) Title = entry.Title.Text;
            if (entry.Summary != null) Summary = entry.Summary.Text;
        }

	}

    public class WebThematicAppAuthor {

        [ApiMember(Name = "AuthorName", Description = "AuthorName", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string AuthorName { get; set; }

		[ApiMember(Name = "AuthorEmail", Description = "AuthorEmail", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string AuthorEmail { get; set; }

		[ApiMember(Name = "AuthorIcon", Description = "AuthorIcon", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string AuthorIcon { get; set; }

		[ApiMember(Name = "AuthorUri", Description = "AuthorUri", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string AuthorUri { get; set; }

        public WebThematicAppAuthor() { }
    }

	public class WebThematicAppCategory {

		[ApiMember(Name = "Term", Description = "Term", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Term { get; set; }

		[ApiMember(Name = "Label", Description = "Label", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Label { get; set; }

		public WebThematicAppCategory() { }
	}

	public class WebThematicAppMapFeature {

		[ApiMember(Name = "MapFeatureName", Description = "MapFeatureName", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string MapFeatureName { get; set; }

		[ApiMember(Name = "MapFeatureTitle", Description = "MapFeatureTitle", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string MapFeatureTitle { get; set; }

		[ApiMember(Name = "MapFeatureUrl", Description = "MapFeatureUrl", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string MapFeatureUrl { get; set; }

		[ApiMember(Name = "MapFeatureAttribution", Description = "MapFeatureAttribution", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string MapFeatureAttribution { get; set; }

		public WebThematicAppMapFeature() { }
	}

	public class WebThematicAppDataContext {

		[ApiMember(Name = "DataContextName", Description = "DataContextName", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string DataContextName { get; set; }

		[ApiMember(Name = "DataContextDescriptionUrl", Description = "DataContextDescriptionUrl", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string DataContextDescriptionUrl { get; set; }

		public WebThematicAppDataContext() { }
	}
}

