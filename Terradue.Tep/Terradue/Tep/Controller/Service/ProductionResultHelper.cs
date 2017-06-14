using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using OpenGis.Wps;
using Terradue.Portal;

namespace Terradue.Tep {
    public class ProductionResultHelper {

		private static readonly log4net.ILog log = log4net.LogManager.GetLogger
			(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static System.Collections.Specialized.NameValueCollection AppSettings = System.Configuration.ConfigurationManager.AppSettings;
        static string recastBaseUrl = AppSettings["RecastBaseUrl"];
        static string statusInProgress = "in progress";
        static string statusCompleted = "completed";
        static string statusError = "error";

        /// <summary>
        /// Gets the wpsjob recast status.
        /// </summary>
        /// <returns>The wpsjob recast status.</returns>
        /// <param name="statusUrl">Status URL.</param>
        public static RecastStatusResponse GetWpsjobRecastStatus(string statusUrl){

			var request = (HttpWebRequest)WebRequest.Create(statusUrl);
			request.Proxy = null;
			request.Method = "GET";

            RecastStatusResponse response;

			using (var remoteWpsResponse = (HttpWebResponse)request.GetResponse()) {
				using (var remotestream = remoteWpsResponse.GetResponseStream()) {
                    response = (RecastStatusResponse)ServiceStack.Text.JsonSerializer.DeserializeFromStream<RecastStatusResponse>(remotestream);
				}
			}

            return response;
		}

        /// <summary>
        /// Gets the wps job recast status URL.
        /// </summary>
        /// <returns>The wps job recast status URL.</returns>
        /// <param name="hostname">Hostname.</param>
        /// <param name="workflow">Workflow.</param>
        /// <param name="runid">Runid.</param>
		public static string GetWpsJobRecastStatusUrl(string hostname, string workflow, string runid) {

			if (string.IsNullOrEmpty(hostname)) throw new Exception("Invalid hostname to get wpsjob result");
			if (string.IsNullOrEmpty(workflow)) throw new Exception("Invalid workflow to get wpsjob result");
			if (string.IsNullOrEmpty(runid)) throw new Exception("Invalid runid to get wpsjob result");

			return string.Format("{0}/t2api/dc/status/{1}/workflows/{2}/runs/{3}", recastBaseUrl, hostname, workflow, runid);
		}

        /// <summary>
        /// Gets the wps job recast describe URL.
        /// </summary>
        /// <returns>The wps job recast describe URL.</returns>
        /// <param name="hostname">Hostname.</param>
        /// <param name="workflow">Workflow.</param>
        /// <param name="runid">Runid.</param>
        public static string GetWpsJobRecastDescribeUrl(string hostname, string workflow, string runid) {
            
			if (string.IsNullOrEmpty(hostname)) throw new Exception("Invalid hostname to get wpsjob result");
			if (string.IsNullOrEmpty(workflow)) throw new Exception("Invalid workflow to get wpsjob result");
			if (string.IsNullOrEmpty(runid)) throw new Exception("Invalid runid to get wpsjob result");

            return string.Format("{0}/t2api/describe/{1}/workflows/{2}/runs/{3}", recastBaseUrl, hostname, workflow, runid);
        }

        /// <summary>
        /// Gets the wpsjob recast response.
        /// </summary>
        /// <returns>The wpsjob recast response.</returns>
        /// <param name="wpsjob">Wpsjob.</param>
        /// <param name="execResponse">Exec response.</param>
        public static ExecuteResponse GetWpsjobRecastResponse(WpsJob wpsjob, ExecuteResponse execResponse = null) {
            if (wpsjob.Status != WpsJobStatus.SUCCEEDED) return execResponse;

            if (execResponse == null) {
				var jobresponse = wpsjob.GetStatusLocationContent();
				if (jobresponse is ExecuteResponse) execResponse = jobresponse as ExecuteResponse;
				else throw new Exception("Error while creating Execute Response of job " + wpsjob.Identifier);
			}
			
			if (execResponse.Status.Item is ProcessSucceededType) {
				var resultUrl = wpsjob.GetResultUrl(execResponse);
				var url = new Uri(resultUrl);

				string hostname = "", workflow = "", runId = "";
				if (url.AbsolutePath.EndsWith("/search") || url.AbsolutePath.EndsWith("/description")) {

					System.Text.RegularExpressions.Regex r;
					System.Text.RegularExpressions.Match m;

					//GET Workflow / RunId for Terradue VMs
					if (url.AbsolutePath.StartsWith("/sbws/wps")) {
						r = new System.Text.RegularExpressions.Regex(@"^\/sbws\/wps\/(?<workflow>[a-zA-Z0-9_\-]+)\/(?<runid>[a-zA-Z0-9_\-]+)\/results");
						m = r.Match(url.AbsolutePath);
						if (m.Success) {
							workflow = m.Result("${workflow}");
							runId = m.Result("${runid}");
							hostname = url.Host;
						}
					} else if (url.AbsolutePath.StartsWith("/sbws/production/run")) {
						r = new System.Text.RegularExpressions.Regex(@"^\/sbws\/production\/run\/(?<workflow>[a-zA-Z0-9_\-]+)\/(?<runid>[a-zA-Z0-9_\-]+)\/products");
						m = r.Match(url.AbsolutePath);
						if (m.Success) {
							hostname = m.Result("${hostname}");
							workflow = m.Result("${workflow}");
							runId = m.Result("${runid}");
						}
					}

					//Get hostname of the run VM
					r = new System.Text.RegularExpressions.Regex(@"^https?:\/\/(?<hostname>[a-zA-Z0-9_\-\.]+)\/");
					m = r.Match(url.AbsoluteUri);
					if (m.Success) {
						hostname = m.Result("${hostname}");
					}
				}

                try {
                    var recaststatusurl = GetWpsJobRecastStatusUrl(hostname, workflow, runId);
                    var recaststatus = GetWpsjobRecastStatus(recaststatusurl);
                    //error during recast
                    if (recaststatus.status == statusError){
                        log.ErrorFormat("Recasting job {0} - url = {1} - message = {2}", wpsjob.Identifier, recaststatusurl, recaststatus.message);
						var exceptionReport = new ExceptionReport {
							Exception = new List<ExceptionType> { new ExceptionType { ExceptionText = new List<string> { "Error while staging data to store --- " + recaststatus.message } } }
						};
						execResponse.Status = new StatusType { Item = new ProcessFailedType { ExceptionReport = exceptionReport }, ItemElementName = ItemChoiceType.ProcessFailed };
                    }

					//recast is still in progress
					else if (recaststatus.status == statusInProgress) { 
                        execResponse.Status = new StatusType { Item = new ProcessStartedType { Value = "Process in progress", percentCompleted = "99" }, ItemElementName = ItemChoiceType.ProcessStarted };
                    }

                    //recast is completed
                    else if (recaststatus.status == statusCompleted){
                        log.DebugFormat("Recasting job {0} - url = {1} - message = {2}", wpsjob.Identifier, recaststatusurl, recaststatus.message);
                        var newStatusLocation = GetWpsJobRecastDescribeUrl(hostname, workflow, runId);
                        execResponse.statusLocation = newStatusLocation;
                        wpsjob.StatusLocation = newStatusLocation;
						wpsjob.Status = WpsJobStatus.STAGED;
                        wpsjob.Store();
                    }

                }catch(Exception e){
                    
                }
			}
			return execResponse;
        }

        /// <summary>
        /// Creates the execute response for staged wpsjob.
        /// </summary>
        /// <returns>The execute response for staged wpsjob.</returns>
        /// <param name="context">Context.</param>
        /// <param name="wpsjob">Wpsjob.</param>
        public static ExecuteResponse CreateExecuteResponseForStagedWpsjob(IfyContext context, WpsJob wpsjob){
            var response = new ExecuteResponse();

            response.Status = new StatusType { Item = new ProcessSucceededType { Value = "Process successful" }, ItemElementName = ItemChoiceType.ProcessSucceeded };
            response.statusLocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + wpsjob.Identifier;
            response.serviceInstance = context.BaseUrl + "/wps/WebProcessingService?REQUEST=GetCapabilities&SERVICE=WPS";
			response.ProcessOutputs = new List<OutputDataType> { };
			response.ProcessOutputs.Add(new OutputDataType {
				Identifier = new CodeType { Value = "result_osd" },
				Item = new DataType {
					Item = new ComplexDataType {
						mimeType = "application/xml",
						Reference = new OutputReferenceType {
                            href = wpsjob.StatusLocation,
							mimeType = "application/opensearchdescription+xml"
						}
					}
				}
			});

			return response;
        }
    }

	[DataContract]
	public class RecastStatusResponse {
        [DataMember]
        public string identifier { get; set; }
		[DataMember]
		public string status { get; set; }
        [DataMember]
		public int percentageCompleted { get; set; }
		[DataMember]
		public string message { get; set; }
	}
}
