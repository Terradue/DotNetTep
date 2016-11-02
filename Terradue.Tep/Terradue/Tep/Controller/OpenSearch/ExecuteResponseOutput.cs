using System;
using System.Collections.Specialized;
using OpenGis.Wps;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep.OpenSearch
{
    class ExecuteResponseOutput : IAtomizable
    {
        OutputDataType o;
        readonly IfyContext context;

        public ExecuteResponseOutput(OutputDataType o, IfyContext context)
        {
            this.context = context;
            this.o = o;
        }

        public AtomItem ToAtomItem(NameValueCollection parameters)
        {

            AtomItem item = new AtomItem(string.Format("{0}", o.Title != null ? o.Title.Value : o.Identifier.Value), "", null, o.Identifier.Value, DateTime.UtcNow);
            item.PublishDate = DateTime.UtcNow;
            item.Identifier = o.Identifier.Value;

            item.Categories.Add(new SyndicationCategory(o.Identifier.Value, null, "wpsoutput"));

            string enclosure = null;

            if (o.Item is DataType && ((DataType)(o.Item)).Item != null)
            {
                if (((DataType)(o.Item)).Item is ComplexDataType)
                {
                    var content = ((DataType)(o.Item)).Item as ComplexDataType;
                    if (content.Reference != null)
                    {
                        var reference = content.Reference as OutputReferenceType;
                        enclosure = reference.href;
                    }
                    item.Summary = new TextSyndicationContent(content.Serialize());
                }
                if (((DataType)(o.Item)).Item is LiteralDataType)
                {
                    var content = ((DataType)(o.Item)).Item as LiteralDataType;
                    item.Summary = new TextSyndicationContent(string.Format("{0} {1} {2}", content.Value, content.uom, content.dataType));
                }
                if (((DataType)(o.Item)).Item is LiteralDataType)
                {
                    var bbox = ((DataType)(o.Item)).Item as BoundingBoxType;
                    item.Summary = new TextSyndicationContent(bbox.Serialize());
                    item.ElementExtensions.Add("box", "http://www.georss.org/georss", string.Format("{0} {1} {2} {3}", 
                                                                                                    bbox.LowerCorner.Trim(' ').Split(' ')[0], bbox.LowerCorner.Trim(' ').Split(' ')[1],
                                                                                                    bbox.UpperCorner.Trim(' ').Split(' ')[0], bbox.UpperCorner.Trim(' ').Split(' ')[1]));
                }
            }
            else if (o.Item is OutputReferenceType)
            {
                var reference = o.Item as OutputReferenceType;
                enclosure = reference.href;
                item.Summary = new TextSyndicationContent(o.Title != null ? o.Title.Value : o.Identifier.Value);
            }

            if ( !string.IsNullOrEmpty(enclosure) )
                item.Links.Add(new Terradue.ServiceModel.Syndication.SyndicationLink(new Uri(enclosure), "search", "Download output " + o.Identifier.Value, "application/octet-stream", 0));

            return item;
        }

        public NameValueCollection GetOpenSearchParameters()
        {
            NameValueCollection nvc = OpenSearchFactory.GetBaseOpenSearchParameter();
            nvc.Set("uid", "{geo:uid}");
            return nvc;
        }
    }
}