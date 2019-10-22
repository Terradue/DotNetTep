using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Terradue.OpenSearch;
using Terradue.Portal;

namespace Terradue.Tep {
    [EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above, AllowsKeywordSearch = true)]
    public class WpsProcessOfferingTep : WpsProcessOffering {
        public WpsProcessOfferingTep(IfyContext context) : base(context) {
        }

        public override object GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
                case "correlatedTo":
                    var settings = MasterCatalogue.OpenSearchFactorySettings;
                    var urlBOS = new UrlBasedOpenSearchable(context, new OpenSearchUrl(value), settings);
                    var entity = urlBOS.Entity;
                    if (entity is EntityList<ThematicApplicationCached>) {
                        var entitylist = entity as EntityList<ThematicApplicationCached>;
                        var items = entitylist.GetItemsAsList();
                        if (items.Count > 0) {
                            var feed = ThematicAppCachedFactory.GetOwsContextAtomFeed(items[0].TextFeed);
                            if (feed != null) {
                                var entry = feed.Items.First();
                                foreach (var offering in entry.Offerings) {
                                    switch (offering.Code) {
                                        case "http://www.opengis.net/spec/owc/1.0/req/atom/wps":
                                            if (offering.Operations != null && offering.Operations.Length > 0) {
                                                foreach (var operation in offering.Operations) {
                                                    var href = operation.Href;
                                                    switch (operation.Code) {
                                                        case "ListProcess":
                                                            var result = new List<KeyValuePair<string, string>>();
                                                            var uri = new Uri(href);
                                                            var nvc = HttpUtility.ParseQueryString(uri.Query);
                                                            foreach (var key in nvc.AllKeys) {
                                                                switch (key) {
                                                                    case "domain":
                                                                        if (nvc[key] != null) {
                                                                            string domainIdentifier = null;
                                                                            if (nvc[key].Contains("${USERNAME}")) {
                                                                                var user = UserTep.FromId(context, context.UserId);
                                                                                user.LoadCloudUsername();
                                                                                domainIdentifier = nvc[key].Replace("${USERNAME}", user.TerradueCloudUsername);
                                                                            } else domainIdentifier = nvc[key];
                                                                            if (!string.IsNullOrEmpty(domainIdentifier)) {
                                                                                var domain = Domain.FromIdentifier(context, domainIdentifier);
                                                                                result.Add(new KeyValuePair<string, string>("DomainId", domain.Id + ""));
                                                                            }
                                                                        }
                                                                        break;
                                                                    case "tag":
                                                                        if (!string.IsNullOrEmpty(nvc[key])) {
                                                                            var tags = nvc[key].Split(",".ToArray());
                                                                            IEnumerable<IEnumerable<string>> permutations = GetPermutations(tags, tags.Count());
                                                                            var r1 = permutations.Select(subset => string.Join("*", subset.Select(t => t).ToArray())).ToArray();
                                                                            var tagsresult = string.Join(",", r1.Select(t => "*" + t + "*"));
                                                                            result.Add(new KeyValuePair<string, string>("Tags", tagsresult));
                                                                        }
                                                                        break;
                                                                    default:
                                                                        break;
                                                                }
                                                            }
                                                            return result;
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
                    return new KeyValuePair<string, string>("DomainId", "-1");//we don't want any result to be returned, as no service is returned to the app (no wps search link)
                default:
                    return base.GetFilterForParameter(parameter, value);
            }
        }

        public static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length) {
            if (length == 1) return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }
    }
}
