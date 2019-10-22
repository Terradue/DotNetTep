using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class GroupServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Get the specified group.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(GetGroup request) {
            WebGroup result;

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/group/{{Id}} GET Id='{0}'", request.Id));
                Group grp = Group.FromId(context, request.Id);
                result = new WebGroup(grp);

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Get list of groups
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(GetGroups request) {
            List<WebGroup> result = new List<WebGroup>();

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/group GET"));
                EntityList<Group> grps = new EntityList<Group>(context);
                grps.Load();
                foreach(Group g in grps) result.Add(new WebGroup(g));

                context.Close();
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Update the specified group.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <returns>the group</returns>
        public object Put(UpdateGroup request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebGroup result;
            try {
                context.Open();
                context.LogInfo(this,string.Format("/group PUT Id='{0}'", request.Id));
                Group grp = (request.Id == 0 ? null : Group.FromId(context, request.Id));
                grp = request.ToEntity(context, grp);
                grp.Store();
                context.LogDebug(this,string.Format("Group {0} updated by user {1}", grp.Name, User.FromId(context, context.UserId).Username));
                result = new WebGroup(grp);
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
        public object Post(CreateGroup request)
        {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebGroup result;
            try{
                context.Open();
                Group grp = (request.Id == 0 ? null : Group.FromId(context, request.Id));
				grp = request.ToEntity(context, grp);
                grp.Store();
                result = new WebGroup(grp);
                context.LogInfo(this,string.Format("/group POST Id='{0}'", grp.Id));
                context.LogDebug(this,string.Format("Group {0} created by user {1}", grp.Name, User.FromId(context, context.UserId).Username));
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
        public object Delete(DeleteGroup request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/group/{{Id}} DELETE Id='{0}'", request.Id));
                Group grp = Group.FromId(context, request.Id);
                if (context.UserLevel == UserLevel.Administrator) grp.Delete();
                else throw new UnauthorizedAccessException(CustomErrorMessages.ADMINISTRATOR_ONLY_ACTION);
                context.LogDebug(this,string.Format("Group {0} deleted by user {1}", grp.Name, User.FromId(context, context.UserId).Username));
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
        public object Post(AddUserToGroup request)
        {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebGroup result;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/group/{{grpId}}/user POST grpId='{0}'", request.GrpId));

                User usr = User.FromId(context, request.Id);
                Group grp = Group.FromId(context, request.GrpId);
                grp.AssignUser (usr);

                result = new WebGroup(grp);
                context.LogDebug(this,string.Format("User {0} has been added to group {1}", usr.Username, grp.Name));
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
        public object Delete(RemoveUserFromGroup request)
        {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebGroup result;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/group/{{grpId}}/user/{{usrId}} DELETE grpId='{0}',usrId='{1}'", request.GrpId, request.UsrId));
                context.RemoveUserFromGroup(request.UsrId, request.GrpId);
                User usr = User.FromId(context, request.UsrId);
                Group grp = Group.FromId(context, request.GrpId);
                result = new WebGroup(grp);
                context.LogDebug(this,string.Format("User {0} has been removed from group {1}", usr.Username, grp.Name));
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }
            return result;
        }

        public object Put(SaveExacltyUsersToGroup request)
        {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            WebGroup result = null;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/group/{{grpId}}/user PUT grpId='{0}'", request.GrpId));
                Group grp = Group.FromId(context, request.GrpId);
                List<User> users = new List<User>();
                foreach(WebUser usr in request) users.Add(User.FromId(context, usr.Id));
                context.LogDebug(this,string.Format("Group {0} has been reset", grp.Name));
                grp.SetUsers(users);
                result = new WebGroup(grp);
                foreach(WebUser usr in request) context.LogDebug(this,string.Format("User {0} has been added to group {1}", usr.Username, grp.Name));
                context.Close ();
            }catch(Exception e) {
                context.LogError(this, e.Message);
                context.Close ();
                throw e;
            }
            return result;
        }

        public object Get(GetUsersFromGroup request)
        {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            List<WebUser> result = new List<WebUser>();;
            try{
                context.Open();
                context.LogInfo(this,string.Format("/group/{{grpId}}/user GET grpId='{0}'", request.GrpId));
                Group grp = Group.FromId(context, request.GrpId);
                foreach(User u in grp.GetUsers()) result.Add(new WebUser(u));
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
        public object Get(UserGetGroupsRequestTep request) {
            List<WebGroup> result = new List<WebGroup>();

            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            try {
                context.Open();
                context.LogInfo(this,string.Format("/user/{{usrId}}/group PUT usrId='{0}'", request.UsrId));

                UserTep user = UserTep.FromIdentifier(context, request.UsrId);
                List<int> ids = user.GetGroups();

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

        public object Get(GroupSearchRequest request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.UserView);
            context.Open();
            context.LogInfo(this, string.Format("/user/search GET"));

            EntityList<Group> groups = new EntityList<Group>(context);
            groups.AddSort("Identifier", SortDirection.Ascending);

            // Load the complete request
            HttpRequest httpRequest = HttpContext.Current.Request;

            OpenSearchEngine ose = MasterCatalogue.OpenSearchEngine;

            Type responseType = OpenSearchFactory.ResolveTypeFromRequest(httpRequest, ose);
            IOpenSearchResultCollection osr = ose.Query(groups, httpRequest.QueryString, responseType);

            OpenSearchFactory.ReplaceOpenSearchDescriptionLinks(groups, osr);

            context.Close();
            return new HttpResult(osr.SerializeToString(), osr.ContentType);
        }

        public object Get(GroupDescriptionRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();
                context.LogInfo(this, string.Format("/user/description GET"));

                EntityList<Group> groups = new EntityList<Group>(context);
                groups.OpenSearchEngine = MasterCatalogue.OpenSearchEngine;

                OpenSearchDescription osd = groups.GetOpenSearchDescription();

                context.Close();

                return new HttpResult(osd, "application/opensearchdescription+xml");
            } catch (Exception e) {
                context.LogError(this, e.Message);
                context.Close();
                throw e;
            }
        }

    }
}

