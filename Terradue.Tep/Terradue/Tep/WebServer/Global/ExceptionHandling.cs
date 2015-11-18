using System;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.WebHost.Endpoints;

namespace Terradue.Tep.WebServer {
    public class ExceptionHandling {

        public static object ServiceExceptionHandler(IHttpRequest httpReq, object request, Exception ex) {
            if (EndpointHost.Config != null && EndpointHost.Config.ReturnsInnerException && ex.InnerException != null && !(ex is IHttpError)) {
                ex = ex.InnerException;
            }
            ResponseStatus responseStatus = ex.ToResponseStatus();


            if (EndpointHost.DebugMode) {
                responseStatus.StackTrace = DtoUtils.GetRequestErrorBody(request) + "\n" + ex;
            }

            var error = DtoUtils.CreateErrorResponse(request, ex, responseStatus);

            IHttpError httpError = error as IHttpError;
            if (httpError != null) {
                if (httpReq.QueryString["errorFormat"] == "json")
                    httpError.ContentType = "application/json";
            }

            return httpError;
        }
    }
}

