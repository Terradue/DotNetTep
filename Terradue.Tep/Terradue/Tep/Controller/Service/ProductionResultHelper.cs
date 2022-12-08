using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Web;
using OpenGis.Wps;
using ServiceStack.Text;
using Terradue.Portal;

namespace Terradue.Tep {
    public class ProductionResultHelper {

		private static readonly log4net.ILog log = log4net.LogManager.GetLogger
			(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static System.Collections.Specialized.NameValueCollection AppSettings = System.Configuration.ConfigurationManager.AppSettings;
        public static string recastBaseUrl = AppSettings["RecastBaseUrl"];
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

            RecastStatusResponse response = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,
                                                                        request.EndGetResponse,
                                                                            null)
            .ContinueWith(task =>
            {
                var httpResponse = (HttpWebResponse)task.Result;
                using (var remotestream = httpResponse.GetResponseStream())
                {
                    return (RecastStatusResponse)ServiceStack.Text.JsonSerializer.DeserializeFromStream<RecastStatusResponse>(remotestream);
                }
            }).ConfigureAwait(false).GetAwaiter().GetResult();

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
                                    if (reference != null) {
                                        reference.href = jobResultUrl;
                                        reference.mimeType = "application/opensearchdescription+xml";
                                        item.Reference = reference;
                                        ((DataType)(output.Item)).Item = item;
                                    }
								} else if (output.Item is OutputReferenceType) {
									context.LogDebug(wpsjob, string.Format("Case result_osd"));
									var reference = output.Item as OutputReferenceType;
                                    if (reference != null) {
                                        reference.href = jobResultUrl;
                                        reference.mimeType = "application/opensearchdescription+xml";
                                        output.Item = reference;
                                    }
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

            if(wpsjob.Status == WpsJobStatus.COORDINATOR){
                log.DebugFormat("GetWpsjobRecastResponse -- Status is Coordinator");
                var resultUrl = WpsJob.GetResultUrl(execResponse);
				if(resultUrl == null) return UpdateProcessOutputs(context, execResponse, wpsjob); 
                wpsjob.StatusLocation = resultUrl;
                wpsjob.Store();
                return CreateExecuteResponseForStagedWpsjob(context, wpsjob, execResponse);
            }
            var supervisorBaseUrl = System.Configuration.ConfigurationManager.AppSettings["SUPERVISOR_WPS_STAGE_URL"];
            if (wpsjob.Status != WpsJobStatus.SUCCEEDED && (supervisorBaseUrl == null || new Uri(supervisorBaseUrl).Host != new Uri(wpsjob.StatusLocation).Host)) {
                log.DebugFormat("GetWpsjobRecastResponse -- Status is not Succeeded");
                return UpdateProcessOutputs(context, execResponse, wpsjob);
            }

            if (execResponse == null) {
				var jobresponse = wpsjob.GetStatusLocationContent();
				if (jobresponse is ExecuteResponse) execResponse = jobresponse as ExecuteResponse;
				else throw new Exception("Error while creating Execute Response of job " + wpsjob.Identifier);
			}

            if (wpsjob.Provider != null && !wpsjob.Provider.StageResults){
                log.DebugFormat("GetWpsjobRecastResponse -- Provider does not allow staging");
                return UpdateProcessOutputs(context, execResponse, wpsjob);
            }
			
			if (execResponse.Status.Item is ProcessSucceededType) {
				var resultUrl = WpsJob.GetResultUrl(execResponse);
				if (resultUrl == null) return UpdateProcessOutputs(context, execResponse, wpsjob);
				var url = new Uri(resultUrl);

				System.Text.RegularExpressions.Regex r;
				System.Text.RegularExpressions.Match m;

				string hostname = url.Host;
                string workflow = "", runId = "";
                string recaststatusurl = "", newStatusLocation = "";                

                //case url is supervisor status url
                if(supervisorBaseUrl != null && url.Host == new Uri(supervisorBaseUrl).Host){
                    wpsjob.StatusLocation = resultUrl;
					// wpsjob.Status = WpsJobStatus.SUCCEEDED;
					wpsjob.Store();
                    return wpsjob.GetExecuteResponseForSucceededJob(execResponse);
                //case url is recast describe url
                } else if(resultUrl.StartsWith(string.Format("{0}/t2api/describe", recastBaseUrl))){
					wpsjob.StatusLocation = resultUrl;
					wpsjob.Status = WpsJobStatus.STAGED;
					wpsjob.Store();
                    return CreateExecuteResponseForStagedWpsjob(context, wpsjob, execResponse);
                } else {
                    //case old sandboxes
    				r = new System.Text.RegularExpressions.Regex(@"^\/sbws\/wps\/(?<workflow>[a-zA-Z0-9_\-]+)\/(?<runid>[a-zA-Z0-9_\-]+)\/results");
    				m = r.Match(url.AbsolutePath);
                    if (m.Success) {
                        workflow = m.Result("${workflow}");
                        runId = m.Result("${runid}");

                        if (wpsjob.Provider != null && wpsjob.Provider.BaseUrl != null) {
                            r = new System.Text.RegularExpressions.Regex(@"https?:\/\/ogc-eo-apps-0?[0-9@].terradue.com");
                            m = r.Match(wpsjob.Provider.BaseUrl);
                            if (m.Success) {
                                if (wpsjob.Owner != null) {
                                    var username = wpsjob.Owner.TerradueCloudUsername;
                                    var recastdescribeurl = string.Format("{0}/t2api/describe/{1}/_results/workflows/{2}/run/{3}", recastBaseUrl, username, workflow, runId);
                                    wpsjob.StatusLocation = recastdescribeurl;
                                    wpsjob.Status = WpsJobStatus.STAGED;
                                    wpsjob.Store();
                                    return CreateExecuteResponseForStagedWpsjob(context, wpsjob, execResponse);
                                }
                            }   
                        }

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
                                if (url.Host == new Uri(recastBaseUrl).Host || CatalogueFactory.IsCatalogUrl(url)) {
                                    log.DebugFormat("Recasting (DIRECT) job {0} - url = {1}", wpsjob.Identifier, resultUrl);
                                    wpsjob.StatusLocation = resultUrl;
                                    wpsjob.Status = WpsJobStatus.STAGED;
                                    wpsjob.Store();
                                    return CreateExecuteResponseForStagedWpsjob(context, wpsjob, execResponse);
                                } else {
                                    //cases external providers
                                    var dataGatewaySubstitutions = JsonSerializer.DeserializeFromString<List<DataGatewaySubstitution>>(AppSettings["DataGatewaySubstitutions"]);
                                    if (dataGatewaySubstitutions != null) {
                                        foreach (var sub in dataGatewaySubstitutions) {
                                            if (url.Host.Equals(sub.host) && url.AbsolutePath.Contains(sub.oldvalue)) {
                                                var path = url.AbsolutePath;
                                                path = path.Replace(sub.oldvalue, sub.substitute);
                                                //we assume that result url is pointing to a metadata file
                                                path = path.Substring(0, path.LastIndexOf("/"));
                                                recaststatusurl = GetWpsJobRecastStatusUrl(path);
                                                newStatusLocation = GetWpsJobRecastDescribeUrl(path);
                                                continue;
                                            }
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
                }

                try {
                    var recaststatus = GetWpsjobRecastStatus(recaststatusurl);
                    //error during recast
                    if (recaststatus.status == statusError){
                        log.ErrorFormat("Recasting job {0} - url = {1} - message = {2}", wpsjob.Identifier, recaststatusurl, recaststatus.message);
						var exceptionReport = new ExceptionReport {
							Exception = new List<ExceptionType> { new ExceptionType { ExceptionText = new List<string> { "Error while staging data to store --- " + recaststatus.message } } }
						};
                        execResponse.Status = new StatusType { Item = new ProcessFailedType { ExceptionReport = exceptionReport }, ItemElementName = ItemChoiceType.ProcessFailed, creationTime = wpsjob.CreatedTime};
                    }

					//recast is still in progress
					else if (recaststatus.status == statusInProgress) { 
                        log.DebugFormat("Recasting STILL IN PROGRESS job {0} - url = {1} - message = {2}", wpsjob.Identifier, recaststatusurl, recaststatus.message);
                        execResponse.Status = new StatusType { Item = new ProcessStartedType { Value = "Process in progress", percentCompleted = "99" }, ItemElementName = ItemChoiceType.ProcessStarted, creationTime = wpsjob.CreatedTime };
                    }

                    //recast is completed
                    else if (recaststatus.status == statusCompleted){
                        log.DebugFormat("Recasting job {0} - url = {1} - message = {2}", wpsjob.Identifier, recaststatusurl, recaststatus.message);
						wpsjob.StatusLocation = newStatusLocation;
						wpsjob.Status = WpsJobStatus.STAGED;
						wpsjob.Store();
                        return CreateExecuteResponseForStagedWpsjob(context, wpsjob, execResponse);
                    }

                }catch(Exception){
                    
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
        public static ExecuteResponse CreateExecuteResponseForStagedWpsjob(IfyContext context, WpsJob wpsjob, ExecuteResponse response){
            if (!string.IsNullOrEmpty(wpsjob.PublishType) && !string.IsNullOrEmpty(wpsjob.PublishUrl))
            {
                //if status url is still recast, we should publish to terrapi
                string recastBaseUrl = AppSettings["RecastBaseUrl"];
                if (!string.IsNullOrEmpty(recastBaseUrl) && new Uri(wpsjob.StatusLocation).Host == new Uri(recastBaseUrl).Host)
                {
                    wpsjob.Publish(wpsjob.PublishUrl, wpsjob.PublishType);
                    return wpsjob.GetExecuteResponseForPublishingJob();
                }
            }
            if (response == null){
                response = new ExecuteResponse();
                response.Status = new StatusType { 
                    Item = new ProcessSucceededType { 
                        Value = "Process successful"
                    }, 
                    ItemElementName = ItemChoiceType.ProcessSucceeded,
                    creationTime = wpsjob.EndTime != DateTime.MinValue ? wpsjob.EndTime : wpsjob.CreatedTime    
                };
            }

            var statusurl = wpsjob.StatusLocation;
            var url = new Uri(statusurl);
            var searchableUrls = JsonSerializer.DeserializeFromString<List<string>>(AppSettings["OpenSearchableUrls"]);
            if (searchableUrls == null || searchableUrls.Count == 0) {
                searchableUrls = new List<string>();
                searchableUrls.Add(recastBaseUrl);//in case appsettings not set
            }
            var recognizedHost = false;
            foreach(var u in searchableUrls) {
                if (new Uri(u).Host == url.Host) recognizedHost = true;
            }
            bool statusNotOpensearchable = 
				!recognizedHost &&
				!statusurl.Contains("/search") &&
				!statusurl.Contains("/description");
            context.LogDebug(wpsjob, string.Format("Status url {0} is opensearchable : {1}", statusurl, statusNotOpensearchable ? "false" : "true"));
			if (statusNotOpensearchable) {
                statusurl = context.BaseUrl + "/job/wps/" + wpsjob.Identifier + "/products/description";
            }

            response.statusLocation = context.BaseUrl + "/wps/RetrieveResultServlet?id=" + wpsjob.Identifier;
            response.serviceInstance = context.BaseUrl + "/wps/WebProcessingService?REQUEST=GetCapabilities&SERVICE=WPS";
			response.ProcessOutputs = new List<OutputDataType> { };
			response.ProcessOutputs.Add(new OutputDataType {
				Identifier = new CodeType { Value = "result_osd" },
				Item = new DataType {
					Item = new ComplexDataType {
						mimeType = "application/xml",
						Reference = new OutputReferenceType {
                            href = statusurl,
							mimeType = "application/opensearchdescription+xml"
						}
					}
				}
			});

            if (!string.IsNullOrEmpty(wpsjob.OwsUrl)){
                response.ProcessOutputs.Add(new OutputDataType {
                    Identifier = new CodeType { Value = "job_ows" },
                    Item = new DataType {
                        Item = new ComplexDataType {
                            mimeType = "application/xml",
                            Reference = new OutputReferenceType {
                                href = wpsjob.OwsUrl,
                                mimeType = "application/xml"
                            }
                        }
                    }
                });

            }

            return response;
        }

        /// <summary>
        /// Is the URL a recast URL.
        /// </summary>
        /// <returns><c>true</c>, if URL is recast URL, <c>false</c> otherwise.</returns>
        /// <param name="url">URL.</param>
        public static bool IsUrlRecastUrl(string url){
            return !string.IsNullOrEmpty(url) && url.StartsWith(recastBaseUrl);
        }

        /// <summary>
        /// Create ExecuteResponse for failed wps job
        /// </summary>
        /// <param name="wpsjob"></param>
        /// <returns></returns>
        public static ExecuteResponse CreateExecuteResponseForFailedWpsjob(WpsJob wpsjob)
        {
            ExecuteResponse response = new ExecuteResponse();
            response.statusLocation = wpsjob.StatusLocation;

            var uri = new Uri(wpsjob.StatusLocation);
            response.serviceInstance = string.Format("{0}://{1}/", uri.Scheme, uri.Host);
            response.service = "WPS";
            response.version = "1.0.0";

            var exceptionReport = new ExceptionReport {
                Exception = new List<ExceptionType> { new ExceptionType { ExceptionText = new List<string> { wpsjob.Logs } } }
            };
            response.Status = new StatusType {
                ItemElementName = ItemChoiceType.ProcessFailed,
                Item = new ProcessFailedType { ExceptionReport = exceptionReport },
                creationTime = wpsjob.CreatedTime
            };
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
