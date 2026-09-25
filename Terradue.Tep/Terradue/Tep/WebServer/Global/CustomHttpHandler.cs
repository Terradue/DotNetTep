using System;
using System.Web;
using System.Web.SessionState;
using ServiceStack.WebHost.Endpoints;

// Temporary for logging
using log4net;
using log4net.Config;

/*! 
 * \namespace Terradue.Tep.WebServer.Common
 * \brief 
 *  
 * The package Terradue.Tep.WebServer.Common contains all the common classes on TepQuickWin.
 * It mainly contains Exception, Errors, Privileges, Context classes.
 */

namespace Terradue.Tep.WebServer
{
	/// <summary>
	/// Session handler decorator.
	/// </summary>
	public class SessionHandlerDecoratorRequire : IHttpHandler, IRequiresSessionState {
		/// <summary>
		/// Gets or sets the handler.
		/// </summary>
		/// <value>The handler.</value>
		private IHttpHandler Handler { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.SessionHandlerDecoratorRequire"/> class.
        /// </summary>
        /// <param name="handler">Handler.</param>
		internal SessionHandlerDecoratorRequire(IHttpHandler handler) {
			this.Handler = handler;
		}
		/// <summary>
		/// Gets a value indicating whether this instance is reusable.
		/// </summary>
		/// <value><c>true</c> if this instance is reusable; otherwise, <c>false</c>.</value>
		public bool IsReusable {
			get { return Handler.IsReusable; }
		}
		/// <summary>
		/// Processes the request.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="context">Context.</param>
		public void ProcessRequest(HttpContext context) {
			Handler.ProcessRequest(context);
		}
	}

	public class SessionHandlerDecoratorReadOnly : IHttpHandler, IReadOnlySessionState {
		/// <summary>
		/// Gets or sets the handler.
		/// </summary>
		/// <value>The handler.</value>
		private IHttpHandler Handler { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.SessionHandlerDecoratorReadOnly"/> class.
        /// </summary>
        /// <param name="handler">Handler.</param>
		internal SessionHandlerDecoratorReadOnly(IHttpHandler handler) {
			this.Handler = handler;
		}
		/// <summary>
		/// Gets a value indicating whether this instance is reusable.
		/// </summary>
		/// <value><c>true</c> if this instance is reusable; otherwise, <c>false</c>.</value>
		public bool IsReusable {
			get { return Handler.IsReusable; }
		}
		/// <summary>
		/// Processes the request.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="context">Context.</param>
		public void ProcessRequest(HttpContext context) {
			Handler.ProcessRequest(context);
		}
	}
	/// <summary>
	/// Session http handler factory.
	/// </summary>
	public class SessionHttpHandlerFactory : IHttpHandlerFactory {
		/// <summary>
		/// The factory.
		/// </summary>
		private readonly static ServiceStackHttpHandlerFactory factory = new ServiceStackHttpHandlerFactory();
		/// <summary>
		/// Gets the handler.
		/// </summary>
		/// <returns>The handler.</returns>
		/// <param name="context">Context.</param>
		/// <param name="requestType">Request type.</param>
		/// <param name="url">URL.</param>
		/// <param name="pathTranslated">Path translated.</param>
		public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated) {
			var handler = factory.GetHandler(context, requestType, url, pathTranslated);
			//CreateLogger();
			if (isLogActive) log.Debug(String.Format("GET HANDLER: url={0}, pathTranslated={1}", url, pathTranslated));

			if (handler == null) {
				if (isLogActive) log.Debug("HANDLER: NULL");
				return null;
			}

			if (
				url.EndsWith("/cb") || 
				url.EndsWith("/auth") || 
				url.EndsWith("/user/emailconfirm") || 
				url.EndsWith("/logout") || 
				url.EndsWith("/user/current") ||
				url.Contains("/user/sso/")
			)
			{
				if (isLogActive) log.Debug("HANDLER: REQUIRES_SESSION");
				return handler == null ? null : new SessionHandlerDecoratorRequire(handler);
			}
			else
			{
				if (isLogActive) log.Debug("HANDLER: READONLY");
				return handler == null ? null : new SessionHandlerDecoratorReadOnly(handler);
			}
		}
		/// <summary>
		/// Releases the handler.
		/// </summary>
		/// <returns>The handler.</returns>
		/// <param name="handler">Handler.</param>
		public void ReleaseHandler(IHttpHandler handler) {
			factory.ReleaseHandler(handler);
		}

		// Temporary for logging, not used if CreateLogger is never called:
		private static ILog log;// = LogManager.GetLogger(typeof(IfyContext));
		private static bool isLogActive = false;

        public virtual void CreateLogger() {
            log4net.Core.Level statLevel = new log4net.Core.Level(50000, "STAT"); // the first and second values must be unique values
			log = LogManager.GetLogger(this.GetType().FullName);
            // adding a new log4net level (statistical level)
            LogManager.GetRepository().LevelMap.Add(statLevel);
            this.LoadLogConfig();
        }

        /// <summary>Create a new TerradueLog instance reading configuration from the default file </summary>
        public void LoadLogConfig() {
            try {
                System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(null);
                System.IO.FileInfo fi = new System.IO.FileInfo(rootWebConfig.AppSettings.Settings["TerradueLogConfigurationFile"].Value);
                XmlConfigurator.Configure(fi);
                isLogActive = true;
            } catch (Exception) {
                isLogActive = false;
            }

        }

	}
}

