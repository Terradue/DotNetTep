using System;
using System.Net;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;

namespace Terradue.Tep.OpenSearch
{
    class SandboxOpenSearchable : GenericOpenSearchable
    {
        public SandboxOpenSearchInformation SandboxOpenSearchInformation { get; private set; }

        private SandboxOpenSearchable(SandboxOpenSearchInformation sosi, OpenSearchableFactorySettings settings) : base(sosi.Url, settings)
        {
            this.SandboxOpenSearchInformation = sosi;
        }

        public static SandboxOpenSearchable CreateSandboxOpenSearchable(OpenSearchUrl osUrl, OpenSearchableFactorySettings settings){


            var sosi = GetSandboxOpenSearchInformation(osUrl);

            return new SandboxOpenSearchable(sosi, settings);
        }

        private static SandboxOpenSearchInformation GetSandboxOpenSearchInformation(OpenSearchUrl url){

            SandboxOpenSearchInformation sosi = new SandboxOpenSearchInformation();

            //check if the url ends with /search or /description
            //if so it can be queried using opensearch, otherise it means that it points to an AtomFeed file
            if (url.AbsolutePath.EndsWith ("/search") || url.AbsolutePath.EndsWith ("/description")) {

                System.Text.RegularExpressions.Regex r;
                System.Text.RegularExpressions.Match m;

                //GET Workflow / RunId for Terradue VMs
                if (url.AbsolutePath.StartsWith ("/sbws/wps")) {
                    r = new System.Text.RegularExpressions.Regex (@"^\/sbws\/wps\/(?<workflow>[a-zA-Z0-9_\-]+)\/(?<runid>[a-zA-Z0-9_\-]+)\/results");
                    m = r.Match (url.AbsolutePath);
                    if (m.Success) {
                        sosi.Workflow = m.Result ("${workflow}");
                        sosi.RunId = m.Result ("${runid}");
                        sosi.Hostname = url.Host;
                        var replacementUrl = url.AbsoluteUri.Replace ("/sbws/wps/"+sosi.Workflow+"/"+sosi.RunId+"/results", "/sbws/production/run/"+sosi.Workflow+"/"+sosi.RunId+"/products");

                        //TEMPORARY: if T2 sandbox (/sbws), use new path (/sbws/production)
                        //if url exists we replace the value
                        try {
                            var request = (HttpWebRequest)WebRequest.Create (replacementUrl);
                            System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,request.EndGetResponse,null)
                            .ContinueWith(task =>
                            {
                                var httpResponse = (HttpWebResponse)task.Result;
                                using (var stream = httpResponse.GetResponseStream()) 
                                {
                                    url = new OpenSearchUrl (replacementUrl);
                                }
                            }).ConfigureAwait(false).GetAwaiter().GetResult();
                        } catch (Exception) { }
                    }
                } else if (url.AbsolutePath.StartsWith ("/sbws/production/run")) { 
                    r = new System.Text.RegularExpressions.Regex (@"^\/sbws\/production\/run\/(?<workflow>[a-zA-Z0-9_\-]+)\/(?<runid>[a-zA-Z0-9_\-]+)\/products");
                    m = r.Match (url.AbsolutePath);
                    if (m.Success) {
                        sosi.Hostname = m.Result ("${hostname}");
                        sosi.Workflow = m.Result ("${workflow}");
                        sosi.RunId = m.Result ("${runid}");
                    }
                }

                //Get hostname of the run VM
                r = new System.Text.RegularExpressions.Regex (@"^https?:\/\/(?<hostname>[a-zA-Z0-9_\-\.]+)\/");
                m = r.Match (url.AbsoluteUri);
                if (m.Success) {
                    sosi.Hostname = m.Result ("${hostname}");
                }
            }

            sosi.Url = url;

            return sosi;

        }
    }

    struct SandboxOpenSearchInformation{

        public OpenSearchUrl Url { get; set; }
        public string Workflow { get; set; }
        public string RunId { get; set; }
        public string Hostname { get; set; }

    }
}