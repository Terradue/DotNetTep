using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using Terradue.OpenNebula;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class GroupServiceTep : ServiceStack.ServiceInterface.Service {
        /// <summary>
        /// Get the specified group.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(GetGroup request) {
            WebGroup result;

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                Group grp = Group.FromId(context, request.Id);
                result = new WebGroup(grp);

                context.Close();
            } catch (Exception e) {
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

            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                EntityList<Group> grps = new EntityList<Group>(context);
                grps.Load();
                foreach(Group g in grps) result.Add(new WebGroup(g));

                context.Close();
            } catch (Exception e) {
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
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            WebGroup result;
            try {
                context.Open();
                Group grp = (request.Id == 0 ? null : Group.FromId(context, request.Id));
                grp = request.ToEntity(context, grp);
                grp.Store();
                result = new WebGroup(grp);
                context.Close();
            } catch (Exception e) {
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
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            WebGroup result;
            try{
                context.Open();
                Group grp = (request.Id == 0 ? null : Group.FromId(context, request.Id));
				grp = request.ToEntity(context, grp);
                grp.Store();

                result = new WebGroup(grp);
                context.Close ();
            }catch(Exception e) {
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
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            try {
                context.Open();
                Group grp = Group.FromId(context, request.Id);
                if (context.UserLevel == UserLevel.Administrator) grp.Delete();
                else throw new UnauthorizedAccessException(CustomErrorMessages.ADMINISTRATOR_ONLY_ACTION);
                context.Close();
            } catch (Exception e) {
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
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            WebGroup result;
            try{
                context.Open();
                context.AddUserToGroup(request.Id, request.GrpId);
                Group grp = Group.FromId(context, request.GrpId);
                result = new WebGroup(grp);
                context.Close ();
            }catch(Exception e) {
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
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            WebGroup result;
            try{
                context.Open();
                context.RemoveUserFromGroup(request.UsrId, request.GrpId);
                Group grp = Group.FromId(context, request.GrpId);
                result = new WebGroup(grp);
                context.Close ();
            }catch(Exception e) {
                context.Close ();
                throw e;
            }
            return result;
        }

        public object Put(SaveExacltyUsersToGroup request)
        {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            WebGroup result = null;
            try{
                context.Open();
                Group grp = Group.FromId(context, request.GrpId);
                List<User> users = new List<User>();
                foreach(WebUser usr in request) users.Add(User.FromId(context, usr.Id));
                grp.SetUsers(users);
                result = new WebGroup(grp);
                context.Close ();
            }catch(Exception e) {
                context.Close ();
                throw e;
            }
            return result;
        }

        public object Get(GetUsersFromGroup request)
        {
            IfyWebContext context = TepWebContext.GetWebContext(PagePrivileges.DeveloperView);
            List<WebUser> result = new List<WebUser>();;
            try{
                context.Open();
                Group grp = Group.FromId(context, request.GrpId);
                foreach(User u in grp.GetUsers()) result.Add(new WebUser(u));
                context.Close ();
            }catch(Exception e) {
                context.Close ();
                throw e;
            }
            return result;
        }

    }
}

