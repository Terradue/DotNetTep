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
    [Route("/login", "GET", Summary = "login", Notes = "Login to the platform with username/password")]
    public class LoginRequestTep : IReturn<Terradue.WebService.Model.WebUser>
	{
		[ApiMember(Name="username", Description = "username", ParameterType = "path", DataType = "String", IsRequired = true)]
		public String username { get; set; }

		[ApiMember(Name="password", Description = "password", ParameterType = "path", DataType = "String", IsRequired = true)]
		public String password { get; set; }

	}

    [Route("/auth", "POST", Summary = "login", Notes = "Login to the platform with username/password")]
    public class LoginAuthRequestTep : IReturn<Terradue.WebService.Model.WebUser>
    {
        [ApiMember(Name="username", Description = "username", ParameterType = "path", DataType = "String", IsRequired = true)]
        public String username { get; set; }

        [ApiMember(Name="password", Description = "password", ParameterType = "path", DataType = "String", IsRequired = true)]
        public String password { get; set; }

    }

    [Route("/logout", "GET", Summary = "logout", Notes = "Logout from the platform")]
    public class LogoutRequestTep : IReturn<WebResponseBool>
	{
	}

    [Route("/auth", "DELETE", Summary = "logout", Notes = "Logout from the platform")]
    public class LogoutAuthRequestTep : IReturn<WebResponseBool>
    {
    }
}
