using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Web;
using System.Web.Services;
using System.Web.SessionState;
using ServiceStack.Common.Web;
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
        /// Get the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        public object Get(LogoutRequestTep request) 
        {
            TepWebContext wsContext = new TepWebContext(PagePrivileges.EverybodyView);
            var redirect = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + "/Shibboleth.sso/Logout";
            try{
                wsContext.Open();
                wsContext.LogInfo(this,string.Format("/logout GET"));
                wsContext.LogDebug(this,string.Format("logout to " + redirect));
                wsContext.EndSession();
                wsContext.Close();
            } catch (ThreadAbortException) {
			} catch (Exception e){
                wsContext.LogError(this, e.Message);
                wsContext.Close();
                throw e;
            }
            
			var redirectResponse = new HttpResult();
			redirectResponse.Headers[HttpHeaders.Location] = redirect;
			redirectResponse.StatusCode = System.Net.HttpStatusCode.Redirect;
			return redirectResponse;
        }

        public object Delete(LogoutAuthRequestTep request) 
        {
            TepWebContext wsContext = new TepWebContext(PagePrivileges.EverybodyView);
            try{
                wsContext.Open();
                wsContext.LogInfo(this,string.Format("/auth DELETE"));
                wsContext.EndSession();
                wsContext.Close();
            }
            catch (Exception e){
                wsContext.LogError(this, e.Message);
                wsContext.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }
	}
}
