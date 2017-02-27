using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using Terradue.Portal;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Tep {

    [EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above)]
    public class ActivityTep : Activity {

        [EntityDataField("id_app")]
        public string AppId { get; set; }

        public ActivityTep(IfyContext context) : base(context) {
        }

        public ActivityTep(IfyContext context, Entity entity, EntityOperationType operation) : base(context, entity, operation) {
        }

        public override AtomItem ToAtomItem(NameValueCollection parameters) {
            if (!IsSearchable(parameters)) return null;

            UserTep owner = (UserTep)UserTep.ForceFromId(context, this.OwnerId);

            //string identifier = null;
            string name = (Entity.Name != null ? Entity.Name : Entity.Identifier);
            string description = null;
            Uri id = new Uri(context.BaseUrl + "/" + this.ActivityEntityType.Keyword + "/search?id=" + Entity.Identifier);

            switch (this.Privilege.Operation) {
            case EntityOperationType.Create:
                description = string.Format("created {0} '{1}'", this.ActivityEntityType.SingularCaption, name);
                break;
            case EntityOperationType.Change:
                description = string.Format("updated {0} '{1}'", this.ActivityEntityType.SingularCaption, name);
                break;
            case EntityOperationType.Delete:
                description = string.Format("deleted {0} '{1}'", this.ActivityEntityType.SingularCaption, name);
                break;
            case EntityOperationType.Share:
                description = string.Format("shared {0} '{1}'", this.ActivityEntityType.SingularCaption, name);
                break;
            default:
                break;
            }

            //AtomItem atomEntry = null;
            AtomItem result = new AtomItem();

            result.Id = id.ToString();
            result.Title = new TextSyndicationContent(base.Entity.Identifier);
            result.Content = new TextSyndicationContent(name);

            result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", Guid.NewGuid());
            result.Summary = new TextSyndicationContent(description);
            result.ReferenceData = this;
            result.PublishDate = this.CreationTime;
            result.LastUpdatedTime = this.CreationTime;
            var basepath = new UriBuilder(context.BaseUrl);
            basepath.Path = "user";
            string usrUri = basepath.Uri.AbsoluteUri + "/" + owner.Username;
            string usrName = (!String.IsNullOrEmpty(owner.FirstName) && !String.IsNullOrEmpty(owner.LastName) ? owner.FirstName + " " + owner.LastName : owner.Username);
            SyndicationPerson author = new SyndicationPerson(owner.Email, usrName, usrUri);
            var ownername = string.IsNullOrEmpty(owner.FirstName) || string.IsNullOrEmpty(owner.LastName) ? owner.Username : owner.FirstName + " " + owner.LastName;
            author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", ownername));
            author.ElementExtensions.Add(new SyndicationElementExtension("avatar", "http://purl.org/dc/elements/1.1/", owner.GetAvatar()));
            result.Authors.Add(author);
            result.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));
            Uri share = new Uri(context.BaseUrl + "/share?url=" + System.Web.HttpUtility.UrlEncode(id.AbsoluteUri) + (!string.IsNullOrEmpty(AppId) ? "&id=" + AppId : ""));
            result.Links.Add(new SyndicationLink(share, "related", "share", "application/atom+xml", 0));

            return result;
        }

        public override KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
            case "correlatedTo":
                var entity = new UrlBasedOpenSearchable(context, new OpenSearchUrl(value), MasterCatalogue.OpenSearchEngine).Entity;
                if (entity is EntityList<ThematicCommunity>) {
                    var entitylist = entity as EntityList<ThematicCommunity>;
                    var items = entitylist.GetItemsAsList();
                    if (items.Count > 0) {
                        return new KeyValuePair<string, string>("DomainId", items[0].Id.ToString());
                    }
                }
                return new KeyValuePair<string, string>();
            default:
                return base.GetFilterForParameter(parameter, value);
            }
        }

    }
}
