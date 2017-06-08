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

        [EntityDataField("params")]
        public string Params { get; set; }

        public ActivityTep(IfyContext context) : base(context) {
        }

        public ActivityTep(IfyContext context, Entity entity, EntityOperationType operation) : base(context, entity, operation) {
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static new ActivityTep FromId(IfyContext context, int id) {
            ActivityTep result = new ActivityTep(context);
            result.Id = id;
            result.Load();
            return result;
        }

        /// <summary>
        /// Froms the entity and privilege.
        /// </summary>
        /// <returns>The entity and privilege.</returns>
        /// <param name="context">Context.</param>
        /// <param name="entity">Entity.</param>
        /// <param name="operation">Operation.</param>
        public static new ActivityTep FromEntityAndPrivilege(IfyContext context, Entity entity, EntityOperationType operation) {
            var etype = EntityType.GetEntityType(entity.GetType());
            var priv = Privilege.Get(EntityType.GetEntityTypeFromId(etype.Id), Privilege.GetOperationType(((char)operation).ToString()));
            ActivityTep result = new ActivityTep(context);
            result.Entity = entity;
            result.EntityTypeId = etype.Id;
            result.Privilege = priv;
            result.Load();
            return result;
        }

        /// <summary>
        /// Adds the parameter.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void AddParam(string key, string value) {
            Params += string.Format("{0}{1}={2}", string.IsNullOrEmpty(Params) ? "" : "&", key, value);
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <returns>The parameters.</returns>
        public NameValueCollection GetParams() {
            NameValueCollection nvc = new NameValueCollection();
            if (Params != null) {
                nvc = System.Web.HttpUtility.ParseQueryString(Params);
            }
            return nvc;
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
            SyndicationPerson author = new SyndicationPerson(null, usrName, usrUri);
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
