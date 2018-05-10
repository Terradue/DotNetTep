using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Xml;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.Portal.OpenSearch;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.ServiceModel.Syndication;
using Terradue.Util;

namespace Terradue.Tep {
    [EntityTable("app_cache", EntityTableConfiguration.Custom, HasDomainReference = true, AllowsKeywordSearch = true)]
    public class ThematicApplicationCached : EntitySearchable {

        [EntityDataField("feed", IsUsedInKeywordSearch = true)]
        public string TextFeed { get; set; }

        [EntityDataField("uid", IsUsedInKeywordSearch = true)]
        public string UId { get; set; }

        protected OwsContextAtomFeed Feed { get; set; }

        public ThematicApplicationCached(IfyContext context) : base(context){}
            
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.ThematicApplicationCached"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="identifier">Identifier.</param>
        /// <param name="domainid">Domainid.</param>
        /// <param name="feed">Feed.</param>
        public ThematicApplicationCached(IfyContext context, string identifier, int domainid, string feed) : base(context) {
            this.UId = identifier;
            this.DomainId = domainid;
            this.TextFeed = feed;
            this.Feed = GetOwsContextAtomFeed(feed);
        }

        /// <summary>
        /// Creates or update the cached app
        /// </summary>
        /// <returns>The or update.</returns>
        /// <param name="context">Context.</param>
        /// <param name="entry">Entry.</param>
        /// <param name="domainid">Domainid.</param>
        public static ThematicApplicationCached CreateOrUpdate(IfyContext context, OwsContextAtomEntry entry, int domainid) {
            var identifier = GetIdentifierFromFeed(entry);
            var appcached = new ThematicApplicationCached(context);
            appcached.UId = identifier;
            appcached.DomainId = domainid;
            try {
                appcached.Load();
            }catch(Exception e){}

            var feed = new OwsContextAtomFeed();
            feed.Items = new List<OwsContextAtomEntry> { entry };

            appcached.Feed = feed;
            appcached.TextFeed = GetOwsContextAtomFeedAsString(feed);
            appcached.Store();

            return appcached;
        }

        public override string GetIdentifyingConditionSql() {
            if (this.UId == null) return null;
            return String.Format("t.uid={0} AND t.id_domain{1}", StringUtils.EscapeSql(this.UId), DomainId == 0 ? " IS NULL" : "=" + DomainId);
        }

        /// <summary>
        /// Gets the cached apps from domain.
        /// </summary>
        /// <returns>The cached apps from domain.</returns>
        /// <param name="context">Context.</param>
        /// <param name="domainid">Domainid.</param>
        public static EntityList<ThematicApplicationCached> GetCachedAppsFromDomain(IfyContext context, int domainid) {
            EntityList<ThematicApplicationCached> result = new EntityList<ThematicApplicationCached>(context);
            if (domainid == 0) result.SetFilter("DomainId", SpecialSearchValue.Null);
            else result.SetFilter("DomainId", domainid+"");
            result.Load();
            return result;
        }

        /// <summary>
        /// Gets the identifier from feed.
        /// </summary>
        /// <returns>The identifier from feed.</returns>
        /// <param name="entry">Entry.</param>
        public static string GetIdentifierFromFeed(OwsContextAtomEntry entry){
            string uid = "";
            var identifiers = entry.ElementExtensions.ReadElementExtensions<string>("identifier", "http://purl.org/dc/elements/1.1/");
            if (identifiers.Count > 0) uid = identifiers[0];
            return uid;
        }

        /// <summary>
        /// Gets the ows context atom feed.
        /// </summary>
        /// <returns>The ows context atom feed.</returns>
        /// <param name="s">S.</param>
        public static OwsContextAtomFeed GetOwsContextAtomFeed(string s) {
            var atomFormatter = new Atom10FeedFormatter();
            XmlReader xmlreader = XmlReader.Create(new StringReader(s));
            atomFormatter.ReadFrom(xmlreader);
            var feed = new OwsContextAtomFeed(atomFormatter.Feed, true);
            return feed;
        }

        /// <summary>
        /// Gets the ows context atom feed.
        /// </summary>
        /// <returns>The ows context atom feed.</returns>
        /// <param name="s">S.</param>
        public static OwsContextAtomFeed GetOwsContextAtomFeed(Stream s) {
            var sr = XmlReader.Create(s);
            Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter();
            atomFormatter.ReadFrom(sr);
            sr.Close();
            var feed = new OwsContextAtomFeed(atomFormatter.Feed, true);
            return feed;
        }

        /// <summary>
        /// Gets the ows context atom feed as string.
        /// </summary>
        /// <returns>The ows context atom feed as string.</returns>
        /// <param name="feed">Feed.</param>
        public static string GetOwsContextAtomFeedAsString(OwsContextAtomFeed feed, string format = "atom") {
            string result = "";
            using (MemoryStream stream = new MemoryStream()) {
                var sw = System.Xml.XmlWriter.Create(stream);
                if (format == "json") {

                } else {
                    Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter((SyndicationFeed)feed);
                    atomFormatter.WriteTo(sw);
                    sw.Flush();
                    sw.Close();
                    stream.Seek(0, SeekOrigin.Begin);
                    StreamReader reader = new StreamReader(stream);
                    result = reader.ReadToEnd();
                }
            }
            return result;
        }

       public override AtomItem ToAtomItem(NameValueCollection parameters) {
            foreach (var key in parameters.AllKeys) {
                switch (key) {
                    case "q":
                        var value = parameters[key].Trim('*');
                        if (!this.TextFeed.Contains(value)) return null;
                        break;
                }
            }

            var atomFormatter = new Atom10FeedFormatter();
            XmlReader xmlreader = XmlReader.Create(new StringReader(TextFeed = this.TextFeed));
            atomFormatter.ReadFrom(xmlreader);
            var feed = new OwsContextAtomFeed(atomFormatter.Feed, true);
            var result = new AtomItem(feed.Items.First());
            return result;
        }

        public bool CanCache {
            get {
                return false;
            }
        }

        #region IEntitySearchable implementation
        public override KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
                case "uid":
                    if (!string.IsNullOrEmpty(value))
                        return new KeyValuePair<string, string>("UId", value);
                    else return new KeyValuePair<string, string>();
                default:
                    return base.GetFilterForParameter(parameter, value);
            }
        }

        #endregion


    }
}
