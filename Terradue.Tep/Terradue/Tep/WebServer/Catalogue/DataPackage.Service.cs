using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;
using System.Linq;



namespace Terradue.Tep.WebServer.Services
{

     [Api("Tep Terradue webserver")]
	[Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
	          EndpointAttributes.Secure   | EndpointAttributes.External | EndpointAttributes.Json)]
	public class DataPackageServiceTep : ServiceStack.ServiceInterface.Service
	{		
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
		/// <summary>
		/// Get the specified request.
		/// </summary>
		/// <param name="request">Request.</param>
		public object Get(GetAllDataPackages request)
		{
			//Get all requests from database
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			List<WebDataPackageTep> result = new List<WebDataPackageTep> ();
			try{
				context.Open();
                context.LogInfo(this,string.Format("/data/package GET"));

                EntityList<DataPackage> tmpList = new EntityList<DataPackage>(context);
                tmpList.Load();
                foreach(DataPackage a in tmpList)
                    result.Add(new WebDataPackageTep(a, context));
				context.Close ();
			}catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
			}

			return result;
		}

		/// <summary>
		/// Get the specified request.
		/// </summary>
		/// <param name="request">Request.</param>
		public object Get(GetDataPackage request)
		{
			//Get all requests from database
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			WebDataPackageTep result;
			try{
				context.Open();
                context.LogInfo(this,string.Format("/data/package/{{Id}} GET Id='{0}", request.Id));

                DataPackage tmp = DataPackage.FromId(context,request.Id);
                result = new WebDataPackageTep(tmp);
				context.Close ();
			}catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
			}

			return result;
		}

        public object Post(DataPackageSaveDefaultRequestTep request)
        {
            //Get all requests from database
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebDataPackageTep result;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/data/package/default POST"));

                DataPackage def = DataPackage.GetTemporaryForCurrentUser(context);
                DataPackage tmp = new DataPackage(context);

                context.StartTransaction();

                if(string.IsNullOrEmpty(request.Identifier) && string.IsNullOrEmpty(request.Name)) throw new Exception("No identifier set");
                //var identifier = !string.IsNullOrEmpty(request.Identifier) ? TepUtility.ValidateIdentifier(request.Identifier) : TepUtility.ValidateIdentifier(request.Name);

                if(request.Overwrite && tmp.OwnerId == context.UserId){
                    tmp = DataPackage.FromNameAndOwner(context, request.Name, context.UserId);
                    foreach(var res in tmp.Resources){
                        res.Delete();
                    }
                } else {
                    tmp = (DataPackage)request.ToEntity(context, tmp);
                    //tmp.Identifier = identifier;
                    try{
                        tmp.Store();
                    }catch(DuplicateEntityIdentifierException e){
                        //tmp = DataPackage.FromIdentifier(context, identifier);
                        //if(tmp.OwnerId == context.UserId){
                            throw new DuplicateNameException(e.Message);
                        //} else {
                        //    throw e;
                        //}
                    }catch(Exception e){
                        if(e.Message.StartsWith("Duplicate entry")) throw new DuplicateNameException(e.Message);
                        throw e;
                    }
                }
  
                foreach(RemoteResource res in def.Resources){
                    RemoteResource tmpres = new RemoteResource(context);
                    tmpres.Location = res.Location;
                    tmp.AddResourceItem(tmpres);
                }
                context.Commit();

                result = new WebDataPackageTep(tmp);
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

        public object Delete(DataPackageClearCurrentDefaultRequestTep request)
        {
            //Get all requests from database
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebDataPackageTep result;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/data/package/default DELETE"));
                DataPackage def = DataPackage.GetTemporaryForCurrentUser(context);

                foreach(RemoteResource res in def.Resources){
                    res.Delete();
                }

                result = new WebDataPackageTep(def);
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

        public object Delete(DataPackageClearDefaultRequestTep request)
        {
            //Get all requests from database
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebDataPackageTep result;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/data/package/default/{{userId}} DELETE userId='{0}", request.userId));
                User user = User.FromId (context, request.userId);
                DataPackage def = DataPackage.GetTemporaryForUser(context, user);

                foreach(RemoteResource res in def.Resources){
                    res.Delete();
                }

                result = new WebDataPackageTep(def);
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

        public object Delete(DataPackageRemoveItemFromDefaultRequestTep request){
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebDataPackageTep result;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/data/package/default/item DELETE"));
                DataPackage def = DataPackage.GetTemporaryForCurrentUser(context);

                foreach(RemoteResource res in def.Resources){
                    var reqDPUri = new UriBuilder(request.Url.Replace("format=atom", "format=json")).Uri.AbsoluteUri;
                    var localDPUri = new UriBuilder(res.Location.Replace("format=atom", "format=json")).Uri.AbsoluteUri;
                    if(reqDPUri.Equals(localDPUri)){
                        res.Delete();
                        break;
                    }
                }

                result = new WebDataPackageTep(def);
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

		/// <summary>
		/// Post the specified request.
		/// </summary>
		/// <param name="request">Request.</param>
        public object Post(DataPackageCreateRequestTep request)
		{
			//Get all requests from database
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			WebDataPackageTep result;
			try{
				context.Open();

                var identifier = !string.IsNullOrEmpty(request.Identifier) ? TepUtility.ValidateIdentifier(request.Identifier) : TepUtility.ValidateIdentifier(request.Name);

                DataPackage tmp = new DataPackage(context);
                tmp = (DataPackage)request.ToEntity(context, tmp);

                if (request.Overwrite && tmp.OwnerId == context.UserId) {
                    tmp = DataPackage.FromIdentifier(context, identifier);
                    foreach (var res in tmp.Resources) {
                        res.Delete();
                    }
                } else {
                    tmp = (DataPackage)request.ToEntity(context, tmp);
                    tmp.Identifier = identifier;
                    try {
                        tmp.Store();
                    } catch (DuplicateEntityIdentifierException e) {
                        tmp = DataPackage.FromIdentifier(context, identifier);
                        if (tmp.OwnerId == context.UserId) {
                            throw new DuplicateNameException(e.Message);
                        } else {
                            throw e;
                        }
                    }
                }

                result = new WebDataPackageTep(tmp);
                context.LogInfo(this,string.Format("/data/package POST Id='{0}'", tmp.Id));
				context.Close ();
			}catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
			}

			return result;
		}

		/// <summary>
		/// Put the specified request.
		/// </summary>
		/// <param name="request">Request.</param>
        public object Put(DataPackageUpdateRequestTep request)
		{
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			WebDataPackageTep result;
			try{
				context.Open();
                context.LogInfo(this,string.Format("/data/package PUT Id='{0}'", request.Id));
                DataPackage tmp;
                if(request.Id != 0) tmp = DataPackage.FromId(context, request.Id);
                else if(!string.IsNullOrEmpty(request.Identifier)) tmp = DataPackage.FromIdentifier(context, request.Identifier);
                else throw new Exception("Undefined data package, set at least Id or Identifier");

                tmp = (DataPackage)request.ToEntity(context, tmp);
				tmp.Store();
                result = new WebDataPackageTep(tmp);
				context.Close ();
			}catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
			}

			return result;
        }

        /// <summary>
        /// Put the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Put(DataPackageExportRequestTep request)
        {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebDataPackageTep result;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/data/package/export PUT Id='{0}'", request.Id == 0 ? request.Identifier : request.Id.ToString()));
                DataPackage tmp;
                if(request.Id != 0) tmp = DataPackage.FromId(context, request.Id);
                else if(!string.IsNullOrEmpty(request.Identifier)) tmp = DataPackage.FromIdentifier(context, request.Identifier);
                else throw new Exception("Undefined data package, set at least Id or Identifier");

                var serie = new Collection(context);
                serie.Identifier = tmp.Identifier;
                serie.Name = tmp.Name;
                var entityType = EntityType.GetEntityType(typeof(DataPackage));
                var description = new UriBuilder(context.BaseUrl + "/" + entityType.Keyword + "/" + tmp.Identifier + "/description");
                description.Query = "key=" + tmp.AccessKey;
                serie.CatalogueDescriptionUrl = description.Uri.AbsoluteUri;
                var user = UserTep.FromId(context, context.UserId);
                serie.Domain = user.Domain;
                serie.Store();
                result = new WebDataPackageTep(tmp);
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Put the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Put(DataPackageUpdateDefaultRequestTep request)
        {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebDataPackageTep result;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/data/package/default PUT"));
                DataPackage def = DataPackage.GetTemporaryForCurrentUser(context);
                //we want to do the copy of a datapackage into the default one
                if(def.Identifier != request.Identifier){
                    foreach(RemoteResource res in def.Resources){
                        res.Delete();
                    }
                    def.LoadItems();
                    var tmp = DataPackage.FromIdentifier(context, request.Identifier);
                    foreach(RemoteResource res in tmp.Resources){
                        RemoteResource tmpres = new RemoteResource(context);
                        tmpres.Location = res.Location;
                        def.AddResourceItem(tmpres);
                    }
                    ActivityTep activity = new ActivityTep(context, tmp, EntityOperationType.View);
                    activity.SetParam("items", tmp.Resources.Count + "");
                    activity.Store();
                }else{
                    def = (DataPackage)request.ToEntity(context, def);    
                }

                result = new WebDataPackageTep(def);
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

        public object Post(DataPackageAddItemToDefaultRequestTep request)
        {
            //Get all requests from database
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebDataPackageTep result;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/data/package/default/item POST Location='{0}'", request.Location));
                DataPackage tmp = DataPackage.GetTemporaryForCurrentUser(context);
                RemoteResource tmp2 = new RemoteResource(context);
                tmp2 = request.ToEntity(context, tmp2);
                tmp.AddResourceItem(tmp2);
                result = new WebDataPackageTep(tmp);
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

        public object Post(DataPackageAddItemsToDefaultRequestTep request)
        {
            //Get all requests from database
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebDataPackageTep result;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/data/package/default/items POST"));
                DataPackage tmp = DataPackage.GetTemporaryForCurrentUser(context);
                foreach(WebDataPackageItem item in request){
                    RemoteResource tmp2 = new RemoteResource(context);
                    tmp2 = item.ToEntity(context, tmp2);
                    tmp.AddResourceItem(tmp2);
                }
                result = new WebDataPackageTep(tmp);
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(DataPackageSearchDefaultRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            IOpenSearchResultCollection result = null;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/package/default/search GET"));
                Terradue.Tep.DataPackage datapackage = DataPackage.GetTemporaryForCurrentUser(context);
                datapackage.SetOpenSearchEngine(MasterCatalogue.OpenSearchEngine);

                OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

                Type responseType = OpenSearchFactory.ResolveTypeFromRequest(HttpContext.Current.Request,ose);

                List<Terradue.OpenSearch.IOpenSearchable> osentities = new List<Terradue.OpenSearch.IOpenSearchable>();
                osentities.AddRange(datapackage.GetOpenSearchableArray());

                var settings = MasterCatalogue.OpenSearchFactorySettings;
                MultiGenericOpenSearchable multiOSE = new MultiGenericOpenSearchable(osentities, settings);
                result = ose.Query(multiOSE, Request.QueryString, responseType);

                context.Close();

            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }

            return new HttpResult(result.SerializeToString(), result.ContentType);
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(DataPackageDescriptionDefaultRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            IOpenSearchResultCollection result = null;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/package/default/description GET"));
                Terradue.Tep.DataPackage datapackage = DataPackage.GetTemporaryForCurrentUser(context);
                datapackage.SetOpenSearchEngine(MasterCatalogue.OpenSearchEngine);
                OpenSearchDescription osd = datapackage.GetLocalOpenSearchDescription();

                context.Close();

                return new HttpResult(osd,"application/opensearchdescription+xml");

            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }

            return new HttpResult(result.SerializeToString(), result.ContentType);
        }

		/// <summary>
		/// Delete the specified request.
		/// </summary>
		/// <param name="request">Request.</param>
        public object Delete(DataPackageDeleteRequestTep request)
		{
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			try{
				context.Open();
                context.LogInfo(this,string.Format("/data/package/{{Identifier}} DELETE Identifier='{0}'", request.Identifier));
                DataPackage tmp = DataPackage.FromIdentifier(context,request.Identifier);
                if(tmp.OwnerId != context.UserId) throw new UnauthorizedAccessException("You are not authorized to delete this data package.");
				tmp.Delete();
				context.Close ();
			}catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
			}

            return Get(new GetAllDataPackages());
		}

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(AddItemToDataPackage request)
        {
            //Get all requests from database
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            WebDataPackageTep result;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/data/package/{{DpId}}/item POST DpId='{0}', Location='{1}'", request.DpId, request.Location));
                DataPackage tmp = DataPackage.FromId(context,request.DpId);
                RemoteResource tmp2 = new RemoteResource(context);
                tmp2 = request.ToEntity(context, tmp2);
                tmp.AddResourceItem(tmp2);
                result = new WebDataPackageTep(tmp);
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }

            return result;
        }

		/// <summary>
		/// Delete the specified request.
		/// </summary>
		/// <param name="request">Request.</param>
		public object Delete(RemoveItemFromDataPackage request)
		{
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
			WebDataPackageTep result;
			try{
				context.Open();
                context.LogInfo(this,string.Format("/data/package/{{DpId}}/item/{{Id}} POST DpId='{0}', Id='{1}'", request.DpId, request.Id));
                RemoteResource tmp = RemoteResource.FromId(context,request.Id);
				tmp.Delete();
                DataPackage dp = DataPackage.FromId(context,request.DpId);
                result = new WebDataPackageTep(dp);
				context.Close ();
			}catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
			}

			return result;
		}

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(DataPackageGetGroupsRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/package/{{DpId}}/group GET DpId='{0}'", request.DpId));
                DataPackage dp = DataPackage.FromIdentifier(context, request.DpId);
                List<int> ids = dp.GetAuthorizedGroupIds().ToList();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    result.Add(new WebGroup(grp));
                }

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Post(DataPackageAddGroupRequestTep request) {

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/package/{{DpId}}/group POST DpId='{0}', Id='{1}'", request.DpId, request.Id));
                DataPackage dp = DataPackage.FromIdentifier(context, request.DpId);

                List<int> ids = dp.GetAuthorizedGroupIds().ToList();

                List<Group> groups = new List<Group>();
                foreach (int id in ids) groups.Add(Group.FromId(context, id));

                foreach(Group grp in groups){
                    if(grp.Id == request.Id) return new WebResponseBool(false);
                }

                dp.GrantPermissionsToGroups(new int[]{request.Id});

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Put the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Put(DataPackageUpdateGroupsRequestTep request) {

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/package/{{DpId}}/group PUT DpId='{0}', Id='{1}'", request.DpId, request.ToArray() != null ? string.Join(",",request.ToArray()) : "null"));
                DataPackage dp = DataPackage.FromIdentifier(context, request.DpId);

                string sql = String.Format("DELETE FROM resourceset_perm WHERE id_resourceset={0} AND id_grp IS NOT NULL;",dp.Id);
                context.Execute(sql);

                dp.GrantPermissionsToGroups(request.ToArray());

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        /// <summary>
        /// Post the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Delete(DataPackageDeleteGroupRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/data/package/{{DpId}}/group DELETE DpId='{0}', Id='{1}'", request.DpId, request.Id));
                DataPackage dp = DataPackage.FromIdentifier(context, request.DpId);

                //TODO: replace once http://project.terradue.com/issues/13954 is resolved
                string sql = String.Format("DELETE FROM resourceset_perm WHERE id_resourceset={0} AND id_grp={1};",dp.Id, request.Id);
                context.Execute(sql);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }


		public object Get(DataPackageGetAvailableIdentifierRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            var available = true;
			try {
				context.Open();
				context.LogInfo(this, string.Format("/data/package/{{DpId}}/available GET DpId='{0}'", request.DpId));
                context.AccessLevel = EntityAccessLevel.Administrator;
                try {
                    DataPackage dp = DataPackage.FromIdentifier(context, request.DpId);
                    available = false;
                }catch(EntityNotFoundException){
                    available = true;
                }catch(Exception){
                    available = false;
                }

				context.Close();
			} catch (Exception e) {
				context.LogError(this, e.Message);
				context.Close();
				throw e;
			}
			return new WebResponseBool(available);
		}
	}
}

