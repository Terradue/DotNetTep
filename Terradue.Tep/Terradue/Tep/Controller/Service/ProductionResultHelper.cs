using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using OpenGis.Wps;
using ServiceStack.Text;
using Terradue.Portal;

namespace Terradue.Tep {
    public class ProductionResultHelper {

		private static readonly log4net.ILog log = log4net.LogManager.GetLogger
			(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static System.Collections.Specialized.NameValueCollection AppSettings = System.Configuration.ConfigurationManager.AppSettings;
        static string recastBaseUrl = AppSettings["RecastBaseUrl"];
        static string catalogBaseUrl = AppSettings["CatalogBaseUrl"];
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
        /// Gets the wps job recast status URL.
        /// </summary>
        /// <returns>The wps job recast status URL.</returns>
        /// <param name="path">Path.</param>
		public static string GetWpsJobRecastStatusUrl(string path) {

			if (string.IsNullOrEmpty(path)) throw new Exception("Invalid path to get wpsjob result");

			return string.Format("{0}/t2api/dc/status/{1}", recastBaseUrl, path);
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
        /// Gets the wps job recast describe URL.
        /// </summary>
        /// <returns>The wps job recast describe URL.</returns>
        /// <param name="path">Path.</param>
		public static string GetWpsJobRecastDescribeUrl(string path) {

			if (string.IsNullOrEmpty(path)) throw new Exception("Invalid path to get wpsjob result");

			return string.Format("{0}/t2api/describe/{1}", recastBaseUrl, path);
		}

        private static ExecuteResponse UpdateProcessOutputs(IfyContext context, ExecuteResponse execResponse, WpsJob wpsjob){
			if (execResponse.ProcessOutputs != null) {
				var jobResultUrl = context.BaseUrl + "/job/wps/" + wpsjob.Identifier + "/products/description";
				foreach (OutputDataType output in execResponse.ProcessOutputs) {
					try {
						if (output.Identifier != null && output.Identifier.Value != null) {
							context.LogDebug(wpsjob, string.Format("Case {0}", output.Identifier.Value));
							if (output.Identifier.Value.Equals("result_metadata") || output.Identifier.Value.Equals("result_osd")) {

								if (output.Item is DataType && ((DataType)(output.Item)).Item != null) {
									var item = ((DataType)(output.Item)).Item as ComplexDataType;
									var reference = item.Reference as OutputReferenceType;
									reference.href = jobResultUrl;
									reference.mimeType = "application/opensearchdescription+xml";
									item.Reference = reference;
									((DataType)(output.Item)).Item = item;
								} else if (output.Item is OutputReferenceType) {
									context.LogDebug(wpsjob, string.Format("Case result_osd"));
									var reference = output.Item as OutputReferenceType;
									reference.href = jobResultUrl;
									reference.mimeType = "application/opensearchdescription+xml";
									output.Item = reference;
								}

								output.Identifier = new CodeType { Value = "result_osd" };
							} else {
								if (output.Item is DataType && ((DataType)(output.Item)).Item != null) {
									var item = ((DataType)(output.Item)).Item as ComplexDataType;
									if (item.Any != null) {
										var reference = new OutputReferenceType();
										reference.mimeType = "application/opensearchdescription+xml";
										reference.href = jobResultUrl;
										item.Reference = reference;
										item.Any = null;
										item.mimeType = "application/xml";
										output.Identifier = new CodeType { Value = "result_osd" };
									}
								}
							}
						}
					} catch (Exception e) {
						context.LogError(wpsjob, e.Message);
					}
				}
			}
	        return execResponse;
        }

        /// <summary>
        /// Gets the wpsjob recast response.
        /// </summary>
        /// <returns>The wpsjob recast response.</returns>
        /// <param name="wpsjob">Wpsjob.</param>
        /// <param name="execResponse">Exec response.</param>
        public static ExecuteResponse GetWpsjobRecastResponse(IfyContext context, WpsJob wpsjob, ExecuteResponse execResponse = null) {
            log.DebugFormat("GetWpsjobRecastResponse");
            if (wpsjob.Status != WpsJobStatus.SUCCEEDED) {
                log.DebugFormat("GetWpsjobRecastResponse -- Status is not Succeeded");
                return UpdateProcessOutputs(context, execResponse, wpsjob);
            }

            if (execResponse == null) {
				var jobresponse = wpsjob.GetStatusLocationContent();
				if (jobresponse is ExecuteResponse) execResponse = jobresponse as ExecuteResponse;
				else throw new Exception("Error while creating Execute Response of job " + wpsjob.Identifier);
			}
			
			if (execResponse.Status.Item is ProcessSucceededType) {
				var resultUrl = wpsjob.GetResultUrl(execResponse);
				var url = new Uri(resultUrl);

				System.Text.RegularExpressions.Regex r;
				System.Text.RegularExpressions.Match m;

				string hostname = url.Host;
                string workflow = "", runId = "";
                string recaststatusurl = "", newStatusLocation = "";

                //case old sandboxes
				r = new System.Text.RegularExpressions.Regex(@"^\/sbws\/wps\/(?<workflow>[a-zA-Z0-9_\-]+)\/(?<runid>[a-zA-Z0-9_\-]+)\/results");
				m = r.Match(url.AbsolutePath);
                if (m.Success) {
					workflow = m.Result("${workflow}");
					runId = m.Result("${runid}");
                    recaststatusurl = GetWpsJobRecastStatusUrl(hostname, workflow, runId);
                    newStatusLocation = GetWpsJobRecastDescribeUrl(hostname, workflow, runId);
                } else {
                    //case new sandboxes
					r = new System.Text.RegularExpressions.Regex(@"^\/sbws\/production\/run\/(?<workflow>[a-zA-Z0-9_\-]+)\/(?<runid>[a-zA-Z0-9_\-]+)\/products");
					m = r.Match(url.AbsolutePath);
                    if (m.Success) {
						workflow = m.Result("${workflow}");
						runId = m.Result("${runid}");
                        recaststatusurl = GetWpsJobRecastStatusUrl(hostname, workflow, runId);
                        newStatusLocation = GetWpsJobRecastDescribeUrl(hostname, workflow, runId);
                    } else {
                        //case production clusters
						r = new System.Text.RegularExpressions.Regex(@"^\/production\/(?<community>[a-zA-Z0-9_\-]+)\/results\/workflows\/(?<workflow>[a-zA-Z0-9_\-]+)\/runs\/(?<runid>[a-zA-Z0-9_\-]+)");
                        m = r.Match(url.AbsolutePath);
                        if (m.Success) {
                            workflow = m.Result("${workflow}");
                            runId = m.Result("${runid}");
                            var community = m.Result("${community}");
                            recaststatusurl = GetWpsJobRecastStatusUrl(hostname, workflow, runId);
                            newStatusLocation = GetWpsJobRecastDescribeUrl(hostname, workflow, runId);
                        } else {
                            //case direct recast or catalog response
                            if (url.Host == new Uri(recastBaseUrl).Host || url.Host == new Uri(catalogBaseUrl).Host) {
                                log.DebugFormat("Recasting (DIRECT) job {0} - url = {1}", wpsjob.Identifier, resultUrl);
                                wpsjob.StatusLocation = resultUrl;
                                wpsjob.Status = WpsJobStatus.STAGED;
                                wpsjob.Store();
                                return CreateExecuteResponseForStagedWpsjob(context, wpsjob);
                            } else {
                                //cases external providers
                                var dataGatewaySubstitutions = JsonSerializer.DeserializeFromString<List<DataGatewaySubstitution>>(AppSettings["DataGatewaySubstitutions"]);
                                foreach (var sub in dataGatewaySubstitutions) {
                                    if (url.Host.Equals(sub.host)) {
                                        var path = url.AbsolutePath;
                                        path = path.Replace(sub.oldvalue, sub.substitute);
                                        //we assume that result url is pointing to a metadata file
                                        path = path.Substring(0, path.LastIndexOf("/"));
										recaststatusurl = GetWpsJobRecastStatusUrl(path);
                                        newStatusLocation = GetWpsJobRecastDescribeUrl(path);
                                        continue;
                                    }
                                }
                                //none of the above cases
                                if (string.IsNullOrEmpty(recaststatusurl)) {
                                    log.DebugFormat("Recasting job {0} - url = {1} ; the url did not match any case", wpsjob.Identifier, url.AbsolutePath);
                                    return UpdateProcessOutputs(context, execResponse, wpsjob);
                                }
                            }
                        }
                    }
                }

                try {
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
                        log.DebugFormat("Recasting STILL IN PROGRESS job {0} - url = {1} - message = {2}", wpsjob.Identifier, recaststatusurl, recaststatus.message);
                        execResponse.Status = new StatusType { Item = new ProcessStartedType { Value = "Process in progress", percentCompleted = "99" }, ItemElementName = ItemChoiceType.ProcessStarted };
                    }

                    //recast is completed
                    else if (recaststatus.status == statusCompleted){
                        log.DebugFormat("Recasting job {0} - url = {1} - message = {2}", wpsjob.Identifier, recaststatusurl, recaststatus.message);
						wpsjob.StatusLocation = newStatusLocation;
						wpsjob.Status = WpsJobStatus.STAGED;
						wpsjob.Store();
                        return CreateExecuteResponseForStagedWpsjob(context, wpsjob);
                    }

                }catch(Exception e){
                    
                }
			}
			return UpdateProcessOutputs(context, execResponse, wpsjob);
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
