using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Web.Services;
using System.Web.SessionState;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services
{
	//-------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------
	
     [Api("Tep Terradue webserver")]
	[Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
	          EndpointAttributes.Secure   | EndpointAttributes.External | EndpointAttributes.Json)]
	/// <summary>
	/// Login service. Used to log into the system (replacing UMSSO for testing)
	/// </summary>
    public class LoginServiceTep : ServiceStack.ServiceInterface.Service {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        		
        /// <summary>
        /// Username/password login
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(LoginRequestTep request) 
		{
            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);
            Terradue.WebService.Model.WebUser response = null;
            Terradue.Portal.User user = null;
			try{
                context.Open();

                user = TepWebContext.passwordAuthenticationType.AuthenticateUser(context, request.username, request.password);
                log.Info(String.Format("Log in from user '{0}'", user.Username));

                response = new Terradue.WebService.Model.WebUser(user);

				context.Close();
			}
			catch (Exception e){
				context.Close();
                throw e;
			}
            return response;
		}

        public object Post(AuthLogin request) 
        {
            TepWebContext context = new TepWebContext(PagePrivileges.EverybodyView);
            Terradue.WebService.Model.WebUser response = null;
            Terradue.Portal.User user = null;
            try{
                context.Open();

                user = TepWebContext.passwordAuthenticationType.AuthenticateUser(context, request.username, request.password);
                response = new Terradue.WebService.Model.WebUser(user);

                context.Close();
            }
            catch (Exception e){
                context.Close();
                throw e;
            }
            return response;
        }

        /// <summary>
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(Logout request) 
        {
            TepWebContext wsContext = new TepWebContext(PagePrivileges.EverybodyView);
            try{
                wsContext.Open();
                log.Info(String.Format("Log out from user '{0}'", wsContext.Username));
                wsContext.EndSession();
                wsContext.Close();
            }
            catch (Exception e){
                wsContext.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Delete(AuthLogout request) 
        {
            TepWebContext wsContext = new TepWebContext(PagePrivileges.EverybodyView);
            try{
                wsContext.Open();
                wsContext.EndSession();
                wsContext.Close();
            }
            catch (Exception e){
                wsContext.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }
	}
}
