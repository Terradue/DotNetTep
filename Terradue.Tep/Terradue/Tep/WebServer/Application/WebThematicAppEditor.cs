﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.Metadata.EarthObservation;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.ServiceModel.Syndication;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer {

	[Route("/app/editor", "POST", Summary = "POST a thematic app", Notes = "")]
	public class ThematicAppEditorPostRequestTep : WebThematicAppEditor { }

    [Route("/app", "DELETE", Summary = "POST a thematic app", Notes = "")]
    public class ThematicAppEditorDeleteRequestTep {
        [ApiMember(Name = "Index", Description = "Index", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string Index { get; set; }

        [ApiMember(Name = "Uid", Description = "Uid", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Uid { get; set; }

        [ApiMember(Name = "UpdateCacheDomain", Description = "Name of the domain to update the cache", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string UpdateCacheDomain { get; set; }

        [ApiMember(Name = "UpdateCache", Description = "UpdateCache", ParameterType = "query", DataType = "bool", IsRequired = true)]
        public bool UpdateCache { get; set; }
    }

    [Route("/app/editor/xml", "POST", Summary = "POST a thematic app to save as xml", Notes = "")]
    public class ThematicAppEditorSaveAsFileRequestTep : WebThematicAppEditor { }

    [Route("/app/editor", "GET", Summary = "POST a thematic app", Notes = "")]
	public class ThematicAppEditorGetRequestTep : IReturn<WebThematicAppEditor> {
		[ApiMember(Name = "Url", Description = "Url", ParameterType = "query", DataType = "string", IsRequired = false)]
		public string Url { get; set; }

		[ApiMember(Name = "Community", Description = "Community", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Community { get; set; }

		[ApiMember(Name = "Uid", Description = "Uid", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Uid { get; set; }
    }

    [Route("/app/editor/file", "POST", Summary = "POST App file")]
    public class LoadAppFromFile : IRequiresRequestStream, IReturn<WebThematicAppEditor> {
        public System.IO.Stream RequestStream { get; set; }
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

		[ApiMember(Name = "GeobrowserPlugins", Description = "GeobrowserPlugins", ParameterType = "query", DataType = "List<WebThematicAppGeobrowserPlugins>", IsRequired = true)]
		public List<WebThematicAppGeobrowserPlugins> GeobrowserPlugins { get; set; }

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
        
		[ApiMember(Name = "WpsServiceOfferingFilters", Description = "WpsServiceOfferingFilters", ParameterType = "query", DataType = "List<WebKeyValue>", IsRequired = true)]
		public List<WebKeyValue> WpsServiceOfferingFilters { get; set; }

		[ApiMember(Name = "ChronogramOfferingPresent", Description = "ChronogramOfferingPresent", ParameterType = "query", DataType = "bool", IsRequired = true)]
		public bool ChronogramOfferingPresent { get; set; }

        [ApiMember(Name = "TimeseriesOfferingPresent", Description = "TimeseriesOfferingPresent", ParameterType = "query", DataType = "bool", IsRequired = true)]
        public bool TimeseriesOfferingPresent { get; set; }

		[ApiMember(Name = "TimebarOfferingPresent", Description = "TimebarOfferingPresent", ParameterType = "query", DataType = "bool", IsRequired = true)]
        public bool TimebarOfferingPresent { get; set; }

		[ApiMember(Name = "StoreUploadOfferingPresent", Description = "StoreUploadOfferingPresent", ParameterType = "query", DataType = "bool", IsRequired = true)]
		public bool StoreUploadOfferingPresent { get; set; }

		[ApiMember(Name = "StoreUploadOfferingContext", Description = "StoreUploadOfferingContext", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string StoreUploadOfferingContext { get; set; }

		[ApiMember(Name = "BaseMaps", Description = "BaseMaps", ParameterType = "query", DataType = "List<string>", IsRequired = true)]
		public List<WebThematicAppBaseMap> BaseMaps { get; set; }

        [ApiMember(Name = "StartDate", Description = "StartDate", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string StartDate { get; set; }

		[ApiMember(Name = "EndDate", Description = "EndDate", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string EndDate { get; set; }

		[ApiMember(Name = "Spatial", Description = "Spatial", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Spatial { get; set; }

		//TODO: content

		[ApiMember(Name = "Index", Description = "Index", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string Index { get; set; }

		[ApiMember(Name = "ApiKey", Description = "ApiKey", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string ApiKey { get; set; }

		[ApiMember(Name = "UpdateCacheDomain", Description = "Name of the domain to update the cache", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string UpdateCacheDomain { get; set; }

        [ApiMember(Name = "UpdateCache", Description = "UpdateCache", ParameterType = "query", DataType = "bool", IsRequired = true)]
        public bool UpdateCache { get; set; }

        public WebThematicAppEditor() { }

        public WebThematicAppEditor(OwsContextAtomEntry entry) {

            //properties
            if (entry.Title != null) Title = entry.Title.Text;
            if (entry.Summary != null) Summary = entry.Summary.Text;
            var identifiers = entry.ElementExtensions.ReadElementExtensions<string>("identifier", OwcNamespaces.Dc);
            if (identifiers.Count() > 0) this.Identifier = identifiers.First();
            if (entry.Date != null) {
				this.StartDate = entry.Date.StartDate.ToString("yyyy-MM-dd");
				this.EndDate = entry.Date.EndDate.ToString("yyyy-MM-dd");
				this.TimebarOfferingPresent = true;
            }

            //authors
            Authors = new List<WebThematicAppAuthor>();
            foreach(var author in entry.Authors){
				var icons = author.ElementExtensions.ReadElementExtensions<string>("icon", "http://www.terradue.com");
                Authors.Add(new WebThematicAppAuthor {
                    AuthorName = author.Name,
                    AuthorEmail = author.Email,
                    AuthorUri = author.Uri,
                    AuthorIcon = icons.Count() > 0 ? icons.First() : null
                });
            }

			//icon
			var icon = entry.Links.FirstOrDefault(l => l.RelationshipType == "icon");
			if (icon != null) this.Icon = icon.Uri.AbsoluteUri;

            //categories
            this.Categories = new List<WebThematicAppCategory>();
            foreach(var cat in entry.Categories){
                switch(cat.Name){
                    case "apptype":
                    case "appstatus":
                    case "keyword":
                        this.Categories.Add(new WebThematicAppCategory{ Label = cat.Label, Term = cat.Name});
                        break;
                    default:
                        this.Categories.Add(new WebThematicAppCategory { Label = cat.Label, Term = cat.Name });
                        break;
                }
            }

            //offerings
            foreach (var offering in entry.Offerings) {
                switch (offering.Code) {
                    case "http://www.terradue.com/spec/owc/1.0/req/atom/datacontext":
                        break;
                    case "http://www.terradue.com/spec/owc/1.0/req/atom/opensearch":
                        this.DataContexts = new List<WebThematicAppDataContext>();
                        foreach (var operation in offering.Operations) {
                            if (operation.Any != null && operation.Any.Length > 0) {
                                var datacontext = new WebThematicAppDataContext();
                                datacontext.DataContextDescriptionUrl = operation.Href;
                                foreach(var any in operation.Any){
                                    switch(any.Name){
                                        case "datacontext":
                                            datacontext.DataContextName = any.InnerText;
                                            break;
                                        case "defaultVisibleItems":
                                            datacontext.DefaultVisibleItems = any.InnerText;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                this.DataContexts.Add(datacontext);
                            }
						}
                        break;
                    case "http://www.terradue.com/spec/owc/1.0/req/atom/opensearchtable":
                        this.OpensearchTableOfferingPresent = true;
                        break;
                    case "http://www.terradue.com/spec/owc/1.0/req/atom/datapackage":
                        this.DataPackageOfferingPresent = true;
                        break;
                    case "http://www.terradue.com/spec/owc/1.0/req/atom/featuresbasket":
                        this.FeatureBasketOfferingPresent = true;
                        break;
                    case "http://www.terradue.com/spec/owc/1.0/req/atom/basemaps":
                        this.BaseMaps = new List<WebThematicAppBaseMap>();
                        foreach (var styleset in offering.StyleSets) { 
                            this.BaseMaps.Add(new WebThematicAppBaseMap{
                                BaseMapName = styleset.Name,
                                BaseMapContent = styleset.Content != null ? styleset.Content.Text : "",
                                BaseMapDefault = styleset.Default,
                                BaseMapType = styleset.Any[0].InnerText
                            });
                        }
                        break;
                    case "http://www.terradue.com/spec/owc/1.0/req/atom/mapfeatures":
                        this.MapFeatures = new List<WebThematicAppMapFeature>();
                        foreach (var styleset in offering.StyleSets){
                            var attribution = styleset.Any.FirstOrDefault(s => s.Name == "attribution");
                            this.MapFeatures.Add(new WebThematicAppMapFeature {
                                MapFeatureType = styleset.Any[0].InnerText,
                                MapFeatureTitle = styleset.Title,
                                MapFeatureName = styleset.Name,
                                MapFeatureDefault = styleset.Default,
                                MapFeatureUrl = styleset.Content != null ? styleset.Content.Href : null,
                                MapFeatureAttribution = attribution != null ? attribution.InnerText : null,
								MapFeatureContentType = styleset.Content != null ? styleset.Content.Type : null
                            });
                        }
                        break;
					case "http://www.terradue.com/spec/owc/1.0/req/atom/geobrowserplugin":
                        if (this.GeobrowserPlugins == null) this.GeobrowserPlugins = new List<WebThematicAppGeobrowserPlugins>();
                        var gbPlugin = new WebThematicAppGeobrowserPlugins();
                        gbPlugin.GeobrowserPluginParameters = new List<WebKeyValue>();
                        foreach (var styleset in offering.StyleSets) {
                            var name = styleset.Name;
                            var type = styleset.Any != null && styleset.Any.Length > 0 ? styleset.Any[0].InnerText : "";
                            var content = styleset.Content != null ? styleset.Content.Text : null;
                            switch(type){
                                case "identifier":
                                    gbPlugin.GeobrowserPluginId = content;
                                    break;
                                case "parameter":
                                    gbPlugin.GeobrowserPluginParameters.Add(new WebKeyValue(name, content));
                                    break;
                                default:
                                    break;
                            }
                        }
                        this.GeobrowserPlugins.Add(gbPlugin);
                        break;
                    case "http://www.opengis.net/spec/owc/1.0/req/atom/wps":
                        this.WpsServiceOfferingPresent = true;
                        if (offering.Operations != null && offering.Operations.Length > 0) {
                            var href = offering.Operations[0].Href;
                            var uri = new Uri(href);
                            var nvc = HttpUtility.ParseQueryString(uri.Query);
							foreach(var key in nvc.AllKeys){
								switch (key) {
									case "format":
										break;
									default:
										if (this.WpsServiceOfferingFilters == null) this.WpsServiceOfferingFilters = new List<WebKeyValue>();
										this.WpsServiceOfferingFilters.Add(new WebKeyValue(key, nvc[key]));
										break;
								}
    						}
                        }
                        break;
                    case "http://www.terradue.com/spec/owc/1.0/req/atom/timeseries":
                        this.TimeseriesOfferingPresent = true;
						break;
                    case "http://www.terradue.com/spec/owc/1.0/req/atom/chronogram":
                        this.ChronogramOfferingPresent = true;
                        break;
					case "http://www.terradue.com/spec/owc/1.0/req/atom/storeupload":
						this.StoreUploadOfferingPresent = true;
						foreach (var operation in offering.Operations) {
							if (operation.Any != null && operation.Any.Length > 0) {
                                this.StoreUploadOfferingContext = operation.Any[0].InnerText;
							}
						}
						break;
                }
            }

			//WKT
            var spatials = entry.ElementExtensions.ReadElementExtensions<string>("spatial", "http://purl.org/dc/terms/");
            if (spatials.Count() > 0) this.Spatial = spatials.First();

            //Index
			var self = entry.Links.FirstOrDefault(l => l.RelationshipType == "self");
			if (self != null) this.Index = ThematicAppCachedFactory.GetIndexFromUrl(self.Uri.AbsoluteUri);
        }

        public OwsContextAtomEntry ToOwsContextAtomEntry(IfyContext context){
            OwsContextAtomEntry entry = new OwsContextAtomEntry();

            if (Identifier == null) throw new Exception("Invalid Identifier");

            //properties
            if (this.Summary != null) entry.Summary = new ServiceModel.Syndication.TextSyndicationContent(this.Summary);
            if (Title != null) entry.Title = new ServiceModel.Syndication.TextSyndicationContent(this.Title);
            entry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);
            if(!string.IsNullOrEmpty(this.StartDate) && !string.IsNullOrEmpty(this.EndDate))
                entry.Date = new DateTimeInterval { StartDate = DateTime.Parse(string.IsNullOrEmpty(this.StartDate) ? this.EndDate : this.StartDate), EndDate = DateTime.Parse(string.IsNullOrEmpty(this.EndDate) ? this.StartDate : this.EndDate) };
            entry.Content = new UrlSyndicationContent(new Uri("/geobrowser/thematicAppContent.html"), "text/html");
            //TODO: content updatable ? get from list ?

            entry.ElementExtensions.Add("type", "http://www.terradue.com", "app");

            //authors
            if (Authors != null) {
                foreach (var author in this.Authors) {
                    var a = new ServiceModel.Syndication.SyndicationPerson {
                        Name = author.AuthorName,
                        Email = author.AuthorEmail,
                        Uri = author.AuthorUri
                    };
                    a.ElementExtensions.Add("icon", "http://www.terradue.com", author.AuthorIcon);
                    entry.Authors.Add(a);
                }
            }

			//links
			entry.Links.Add(new ServiceModel.Syndication.SyndicationLink {
				MediaType = "application/xml",
				RelationshipType = "self",
                Title = "self link",
                Uri = new Uri(context.GetConfigValue("catalog-baseurl") + "/" + this.Index + "?format=atom&uid=" + this.Identifier)
			});
			if (!string.IsNullOrEmpty(this.Icon)) {
				try {
					entry.Links.Add(new ServiceModel.Syndication.SyndicationLink {
						MediaType = "image/png",
						RelationshipType = "icon",
						Uri = new Uri(this.Icon)
					});
				}catch(Exception){}
            }

            //categories
            bool hasAppCat = false;
            if (this.Categories != null) {
                foreach (var cat in this.Categories) {
                    entry.Categories.Add(new ServiceModel.Syndication.SyndicationCategory {
                        Label = cat.Label,
                        Name = cat.Term
                    });
                    if (cat.Label == "App") hasAppCat = true;
                }
            }
            //Add default category "App"
            if (!hasAppCat) {
                entry.Categories.Add(new ServiceModel.Syndication.SyndicationCategory {
                    Label = "App",
                    Name = "App"
                });
            }

            //offerings
            List<OwcOffering> offerings = new List<OwcOffering>();
            var doc = new System.Xml.XmlDocument();
            if(OpensearchTableOfferingPresent){
                offerings.Add(new OwcOffering{
                    Code = "http://www.terradue.com/spec/owc/1.0/req/atom/opensearchtable"
                });
            }
			if (DataPackageOfferingPresent) {
				offerings.Add(new OwcOffering {
					Code = "http://www.terradue.com/spec/owc/1.0/req/atom/datapackage",
                    Operations = new OwcOperation[]{
                        new OwcOperation{
                            Code = "Search",
                            Type = "application/json",
                            Href = "file:///t2api/data/package/search?format=json"
                        }
                    }
				});
			}
            if (FeatureBasketOfferingPresent) {
				offerings.Add(new OwcOffering {
					Code = "http://www.terradue.com/spec/owc/1.0/req/atom/featuresbasket",
					Operations = new OwcOperation[]{
						new OwcOperation{
							Code = "Description",
							Type = "application/opensearchdescription+xml",
							Href = "file:///t2api/data/package/default/description"
						}
					}
				});
			}
            if (WpsServiceOfferingPresent) {
				string query = "";
				if(this.WpsServiceOfferingFilters != null){
					foreach(var filter in this.WpsServiceOfferingFilters){
						query += (query == "" ? "?" : "&") + filter.Key + "=" + filter.Value;
					}
				}
				offerings.Add(new OwcOffering {
					Code = "http://www.opengis.net/spec/owc/1.0/req/atom/wps",
					Operations = new OwcOperation[]{
						new OwcOperation{
							Code = "ListProcess",
							Type = "application/json",
                            Href = "file:///t2api/service/wps/search" + query
						}
					}
				});
			}
			if (ChronogramOfferingPresent) {
				offerings.Add(new OwcOffering {
					Code = "http://www.terradue.com/spec/owc/1.0/req/atom/chronogram"
				});
			}
            if (TimeseriesOfferingPresent) {
                offerings.Add(new OwcOffering {
                    Code = "http://www.terradue.com/spec/owc/1.0/req/atom/timeseries"
                });
            }
			if (StoreUploadOfferingPresent) {
                var datacontext = doc.CreateElement("datacontext");
                datacontext.InnerText = StoreUploadOfferingContext;
				offerings.Add(new OwcOffering {
					Code = "http://www.terradue.com/spec/owc/1.0/req/atom/storeupload",
					Operations = new OwcOperation[]{
						new OwcOperation{
                            Code = "Search",
                            Type = "application/json",
                            Href = "https://www.terradue.com",
							Any = new System.Xml.XmlElement[]{ datacontext }
						}
					}
				});
			}
            if (this.DataContexts != null && this.DataContexts.Count > 0){
                var dcStylesets = new List<OwcStyleSet>();
                var osOperations = new List<OwcOperation>();
                foreach(var dc in this.DataContexts){
                    dcStylesets.Add(new OwcStyleSet{
                        Name = dc.DataContextName,
                        Title = dc.DataContextName
                    });
					var any = new List<System.Xml.XmlElement>();
					if (!string.IsNullOrEmpty(dc.DataContextName)) {
						var datacontext = doc.CreateElement("datacontext", "http://www.terradue.com");
						datacontext.InnerText = dc.DataContextName;
						any.Add(datacontext);
					}
					if (!string.IsNullOrEmpty(dc.DefaultVisibleItems)) {
						var defaultVisibleItems = doc.CreateElement("defaultVisibleItems", "http://www.terradue.com");
						defaultVisibleItems.InnerText = dc.DefaultVisibleItems;
						any.Add(defaultVisibleItems);
					}
                    var isDescription = CatalogueFactory.IsUrlOpensearchDescription(dc.DataContextDescriptionUrl);
                    if (isDescription){
                        osOperations.Add(new OwcOperation {
                            Code = "DescribeDataset",
                            Type = "application/opensearchdescription+xml",
                            Href = dc.DataContextDescriptionUrl,
                            Any = any.ToArray()
                        });
                    } else {
                        var isListSeries = CatalogueFactory.IsUrlListSeries(dc.DataContextDescriptionUrl);
                        if (isListSeries) {
                            osOperations.Add(new OwcOperation {
                                Code = "ListSeries",
                                Type = "application/json",
                                Href = dc.DataContextDescriptionUrl,
                                Any = any.ToArray()
                            });
                        } else {
                            var isSearch = CatalogueFactory.IsUrlOpensearchSearch(dc.DataContextDescriptionUrl);
                            if (isSearch) {
                                osOperations.Add(new OwcOperation {
                                    Code = "SearchDataset",
                                    Type = "application/atom+xml",
                                    Href = dc.DataContextDescriptionUrl,
                                    Any = any.ToArray()
                                });
                            }
                        }
                    }
                }
                offerings.Add(new OwcOffering{
                    Code = "http://www.terradue.com/spec/owc/1.0/req/atom/opensearch",
                    Operations = osOperations.ToArray()
                });
				offerings.Add(new OwcOffering {
					Code = "http://www.terradue.com/spec/owc/1.0/req/atom/datacontext",
                    StyleSets = dcStylesets.ToArray()
				});
            }
            if (this.BaseMaps != null && this.BaseMaps.Count > 0) {
                var bmStylesets = new List<OwcStyleSet>();
                foreach (var bm in this.BaseMaps) {
					var type = doc.CreateElement("type","http://www.terradue.com");
                    type.InnerText = bm.BaseMapType;
					bmStylesets.Add(new OwcStyleSet {
                        Default = bm.BaseMapDefault,
						Name = bm.BaseMapName,
                        Abstract = bm.BaseMapName,
                        Any = new System.Xml.XmlElement[] { type },
                        Content = new OwcContent{ Text = bm.BaseMapContent }
					});
                }
				offerings.Add(new OwcOffering {
					Code = "http://www.terradue.com/spec/owc/1.0/req/atom/basemaps",
					StyleSets = bmStylesets.ToArray()
				});
            }
            if (this.MapFeatures != null && this.MapFeatures.Count > 0) {
				var mfStylesets = new List<OwcStyleSet>();
                foreach (var mf in this.MapFeatures){
					var any = new List<System.Xml.XmlElement>();               
					if (!string.IsNullOrEmpty(mf.MapFeatureType)) {
						var type = doc.CreateElement("type", "http://www.terradue.com");
                        type.InnerText = mf.MapFeatureType;
                        any.Add(type);
                    }
					if (!string.IsNullOrEmpty(mf.MapFeatureAttribution)) {
                        var attribution = doc.CreateElement("attribution", "http://www.terradue.com");
						attribution.InnerText = mf.MapFeatureAttribution;
						any.Add(attribution);
                    }
					mfStylesets.Add(new OwcStyleSet {
						Default = mf.MapFeatureDefault,
                        Title = mf.MapFeatureTitle,
                        Name = mf.MapFeatureName,
                        Abstract = mf.MapFeatureName,
						Any = any.ToArray(),
						Content = new OwcContent { Href = mf.MapFeatureUrl, Type = string.IsNullOrEmpty(mf.MapFeatureContentType) ? "image/png" : mf.MapFeatureContentType }
					});
                }            
				offerings.Add(new OwcOffering {
					Code = "http://www.terradue.com/spec/owc/1.0/req/atom/mapfeatures",
					StyleSets = mfStylesets.ToArray()
				});
			}
			if (this.GeobrowserPlugins != null && this.GeobrowserPlugins.Count > 0) {
				foreach (var gbp in this.GeobrowserPlugins) {
                    var mfStylesets = new List<OwcStyleSet>();
                    var any = new List<System.Xml.XmlElement>();
					var type = doc.CreateElement("type", "http://www.terradue.com");
					type.InnerText = "identifier";
                    any.Add(type);
                    mfStylesets.Add(new OwcStyleSet {
                        Name = "pluginId",
                        Any = any.ToArray(),
                        Content = new OwcContent { Text = gbp.GeobrowserPluginId }
                    });
                    if(gbp.GeobrowserPluginParameters != null){
                        foreach(var gbpparam in gbp.GeobrowserPluginParameters){
                            any = new List<System.Xml.XmlElement>();
                            type = doc.CreateElement("type", "http://www.terradue.com");
                            type.InnerText = "parameter";
                            any.Add(type);
                            mfStylesets.Add(new OwcStyleSet {
                                Name = gbpparam.Key,
                                Any = any.ToArray(),
                                Content = new OwcContent { Text = gbpparam.Value }
                            });
                        }
                    }
                    offerings.Add(new OwcOffering {
                        Code = "http://www.terradue.com/spec/owc/1.0/req/atom/geobrowserplugin",
                        StyleSets = mfStylesets.ToArray()
                    });
                }
            }
            entry.Offerings = offerings;

            //spatial
            if(!string.IsNullOrEmpty(this.Spatial)) entry.ElementExtensions.Add("spatial", "http://purl.org/dc/terms/", this.Spatial);

			return entry;
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

		[ApiMember(Name = "MapFeatureType", Description = "MapFeatureType", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string MapFeatureType { get; set; }

        [ApiMember(Name = "MapFeatureName", Description = "MapFeatureName", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string MapFeatureName { get; set; }

		[ApiMember(Name = "MapFeatureTitle", Description = "MapFeatureTitle", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string MapFeatureTitle { get; set; }

		[ApiMember(Name = "MapFeatureUrl", Description = "MapFeatureUrl", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string MapFeatureUrl { get; set; }

		[ApiMember(Name = "MapFeatureContentType", Description = "MapFeatureContentType", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string MapFeatureContentType { get; set; }

		[ApiMember(Name = "MapFeatureAttribution", Description = "MapFeatureAttribution", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string MapFeatureAttribution { get; set; }

        [ApiMember(Name = "MapFeatureDefault", Description = "MapFeatureDefault", ParameterType = "query", DataType = "bool", IsRequired = true)]
        public bool MapFeatureDefault { get; set; }

		public WebThematicAppMapFeature() { }
	}

	public class WebThematicAppGeobrowserPlugins {

        [ApiMember(Name = "GeobrowserPluginId", Description = "GeobrowserPluginId", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string GeobrowserPluginId { get; set; }

        [ApiMember(Name = "GeobrowserPluginsParameters", Description = "GeobrowserPluginsParameters", ParameterType = "query", DataType = "List<WebKeyValue>", IsRequired = true)]
        public List<WebKeyValue> GeobrowserPluginParameters { get; set; }
              
		public WebThematicAppGeobrowserPlugins() { }
		
	}

	public class WebThematicAppDataContext {

		[ApiMember(Name = "DataContextName", Description = "DataContextName", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string DataContextName { get; set; }

		[ApiMember(Name = "DataContextDescriptionUrl", Description = "DataContextDescriptionUrl", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string DataContextDescriptionUrl { get; set; }

        [ApiMember(Name = "defaultVisibleItems", Description = "DefaultVisibleItems", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string DefaultVisibleItems { get; set; }

		public WebThematicAppDataContext() { }
	}

	public class WebThematicAppBaseMap {

		[ApiMember(Name = "BaseMapName", Description = "BaseMapName", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string BaseMapName { get; set; }

		[ApiMember(Name = "BaseMapDefault", Description = "BaseMapDefault", ParameterType = "query", DataType = "bool", IsRequired = true)]
		public bool BaseMapDefault { get; set; }

		[ApiMember(Name = "BaseMapContent", Description = "BaseMapContent", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string BaseMapContent { get; set; }

		[ApiMember(Name = "BaseMapType", Description = "BaseMapType", ParameterType = "query", DataType = "string", IsRequired = true)]
		public string BaseMapType { get; set; }

		public WebThematicAppBaseMap() { }
	}
}

