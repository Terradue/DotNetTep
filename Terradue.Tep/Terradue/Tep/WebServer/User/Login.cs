using System;
using System.Data;
using System.ComponentModel;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using Terradue.Portal;
using System.Web.Services;
using System.Web.SessionState;
using System.Diagnostics;
using Terradue.Tep.WebServer;
using ServiceStack;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer
{

    [Route("/logout", "GET", Summary = "logout", Notes = "Logout from the platform")]
    public class LogoutRequestTep : IReturn<WebResponseBool>
	{
	}

    [Route("/auth", "DELETE", Summary = "logout", Notes = "Logout from the platform")]
    public class LogoutAuthRequestTep : IReturn<WebResponseBool>
    {
    }
}
