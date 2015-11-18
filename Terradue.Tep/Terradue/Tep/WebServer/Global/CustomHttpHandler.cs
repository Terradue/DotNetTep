using System;
using System.Web;
using System.Web.SessionState;
using ServiceStack.WebHost.Endpoints;

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
	public class SessionHandlerDecorator : IHttpHandler, IRequiresSessionState {
		/// <summary>
		/// Gets or sets the handler.
		/// </summary>
		/// <value>The handler.</value>
		private IHttpHandler Handler { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.Common.SessionHandlerDecorator"/> class.
        /// </summary>
        /// <param name="handler">Handler.</param>
		internal SessionHandlerDecorator(IHttpHandler handler) {
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
			return handler == null ? null : new SessionHandlerDecorator(handler);
		}
		/// <summary>
		/// Releases the handler.
		/// </summary>
		/// <returns>The handler.</returns>
		/// <param name="handler">Handler.</param>
		public void ReleaseHandler(IHttpHandler handler) {
			factory.ReleaseHandler(handler);
		}
	}
}

