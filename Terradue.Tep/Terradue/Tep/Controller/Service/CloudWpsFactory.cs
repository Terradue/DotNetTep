using System;
using Terradue.Portal;
using Terradue.OpenNebula;
using System.Collections.Generic;
using System.Xml;
using Terradue.Cloud;


/*!
\defgroup CloudWpsFactory CloudWpsFactory
@{

This component enables Cloud Appliance that exposes a WPS service to be exposed as a processing service automatically.

\ingroup Tep

\xrefitem dep "Dependencies" "Dependencies" calls \ref CloudProvider to retrieve the computing appliances

\xrefitem dep "Dependencies" "Dependencies" calls \ref OneClient to contact OpenNebula interface

\xrefitem dep "Dependencies" "Dependencies" creates \ref WpsProvider for each computing appliance that expose WPS 

\xrefitem dep "Dependencies" "Dependencies" creates \ref WpsService for each process found in GetCapabilities of the WPS Service

Using the \ref OneClient to the cloud, it retrieve the VMs that expose WPS interface and load the services.
The following diagrams describes how the WPS services are discovered and offered to the users dynamically. 
Only operations that differs from the ones described in \ref WpsProvider module are illustrated.

\startuml{wpsprovider.png}
!define DIAG_NAME WPS Service Cloud Discovery Sequence Diagram

participant "WebClient" as WC
participant "WebServer" as WS
participant "Provider" as P << Virtual Machine >>
participant "Cloud Provider" as C << OpenNebula >>

autonumber

== Get Capabilities ==

WC -> WS: GetCapabilities request
activate WS
WS -> C: List all VMs that are tagged as WPS provider
WS -> WS: create temporary WPS providers
loop on each provider
    WS -> P: GetCapabilities
    WS -> WS: extract services from GetCapabilities using remote identifier
    loop on each service
        WS -> WS: get service info (identifier, title, abstract)
    end
end
WS -> WS: aggregate all services info into response offering
WS -> WC: return aggregated GetCapabilities
deactivate WS

== Describe Process ==

WC -> WS: DescribeProcess request
activate WS
WS -> C: get service provider
WS -> P: GetCapabilities
WS -> WS: extract describeProcess url from GetCapabilities using remote identifier
WS -> WS: build describeProcess request
WS -> P: DescribeProcess
WS -> WC: return result from describeProcess
deactivate WS

== Execute ==

WC -> WS: Execute request
activate WS
WS -> C: get service provider
WS -> P: GetCapabilities
WS -> WS: extract execute url from GetCapabilities using remote identifier
WS -> WS: build execute request
WS -> P: Execute
alt case error
    WS -> WC: return error
else case success
    WS -> DB: store job
    WS -> WS: update job RetrieveResultServlet url
    WS -> WC: return created job
end
deactivate WS

== Search WPS process ==

WC -> WS: WPS search request
activate WS
WS -> C: Load all Providers
loop on each provider
    WS -> P: GetCapabilities
    WS -> WS: get services info
    loop on each service
        WS -> WS: create local identifier and cache remote identifier
        WS -> WS: use local server url as baseurl
        WS -> WS: add service info to the response
    end
end
deactivate WS


footer
DIAG_NAME
(c) Terradue Srl
endfooter
\enduml
\ingroup core


@}
 */

namespace Terradue.Tep.Controller {

    /// \ingroup CloudWpsFactory
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    public class CloudWpsFactory {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string cloudusername { get; set; }

        private OneClient oneclient { get; set; }
        private OneClient oneClient { 
            get{ 
                if (oneclient == null) {
                    oneclient = oneCloud.XmlRpc;
                }
                oneclient.StartDelegate(cloudusername);
                return oneclient;
            } 
        }
        private OneCloudProvider onecloud { get; set; }
        private OneCloudProvider oneCloud { 
            get{ 
                if(onecloud==null) onecloud = (OneCloudProvider)CloudProvider.FromId(context, context.GetConfigIntegerValue("One-default-provider"));
                return onecloud;
            }
        }

        private IfyContext context { get; set; }

        public void StartDelegate(int idusr){
            CloudUser cuser = CloudUser.FromIdAndProvider(context, idusr, oneCloud.Id);
            cloudusername = cuser.CloudUsername;
        }

        public void EndDelegate(string username){
            CloudUser cuser = CloudUser.FromIdAndProvider(context, context.UserId, oneCloud.Id);
            cloudusername = cuser.CloudUsername;
        }

        public CloudWpsFactory(IfyContext context) {
            this.context = context;
        }

        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public List<WpsProvider> GetWPSFromVMs(){
            if (context.UserId == 0) return new List<WpsProvider>();

            List<WpsProvider> result = new List<WpsProvider>();
            try{
                StartDelegate(context.UserId);
                log.Debug(string.Format("Get VM Pool for user {0}",cloudusername));
                VM_POOL pool = oneClient.VMGetPoolInfo(-2, -1, -1, 3);
                if(pool != null && pool.VM != null){
                    log.Debug(string.Format("{0} VM found",pool.VM.Length));
                    foreach (VM vm in pool.VM) {
                        if(vm.USER_TEMPLATE == null) continue;
                        try{
                            XmlNode[] user_template = (XmlNode[])vm.USER_TEMPLATE;
                            foreach(XmlNode nodeUT in user_template){
                                if(nodeUT.Name == "WPS"){
                                    log.Debug(string.Format("WPS found : {0} - {1}",vm.ID, vm.GNAME));
                                    result.Add(CreateWpsProviderForOne(context, vm));
                                    break;
                                }
                            }
                        }catch(Exception e){

                        }
                    }
                }
            }catch(System.Net.WebException e){
            }
            return result;
        }

        /// <summary>
        /// Creates the wps provider from VM id (OpenNebula).
        /// </summary>
        /// <returns>The wps provider</returns>
        /// <param name="id">Id of the Virtual Machine.</param>
        public WpsProvider CreateWpsProviderForOne(string id){
            VM vm = oneClient.VMGetInfo(Int32.Parse(id));
            return CreateWpsProviderForOne(context, vm);
        }

        /// <summary>
        /// Creates the wps provider from VM (OpenNebula).
        /// </summary>
        /// <returns>The wps provider</returns>
        /// <param name="vm">Virtual machine object.</param>
        public static WpsProvider CreateWpsProviderForOne(IfyContext context, VM vm){
            WpsProvider wps = new WpsProvider(context);
            wps.Name = vm.NAME;
            wps.Identifier = "one-" + vm.ID;
            wps.Proxy = true;

            wps.Description = vm.UNAME + " from laboratory " + vm.GNAME;
            XmlNode[] template = (XmlNode[])vm.TEMPLATE;
            foreach (XmlNode nodeT in template) {
                if (nodeT.Name == "NIC") {
                    wps.BaseUrl = String.Format("http://{0}:8080/wps/WebProcessingService" , nodeT["IP"].InnerText);
                    break;
                }
            }
            return wps;
        }

        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public WpsProcessOffering CreateWpsProcessOfferingForOne(string vmId, string processId){
            WpsProvider wps = this.CreateWpsProviderForOne(vmId);

            var wpsUri = new UriBuilder(wps.BaseUrl);
            wpsUri.Query = "service=WPS&request=GetCapabilities";

            foreach (WpsProcessOffering process in wps.GetWpsProcessOfferingsFromUrl(wpsUri.Uri.AbsoluteUri)) {
                if (process.RemoteIdentifier.Equals(processId)) return process;
            }

            return null;
        }

        /// <summary>
        /// Gets the wps process offering from DB or from cloud
        /// </summary>
        /// <returns>The wps process offering.</returns>
        /// <param name="identifier">Identifier.</param>
        public static WpsProcessOffering GetWpsProcessOffering(IfyContext context, string identifier){
            WpsProcessOffering wps = null;
            try {
                //wps is stored in DB
                wps = (WpsProcessOffering)WpsProcessOffering.FromIdentifier(context, identifier);
            } catch (Exception e) {
                //wps is not stored in DB
                string[] identifierParams = identifier.Split("-".ToCharArray());
                if (identifierParams.Length == 3) {
                    switch (identifierParams[0]) {
                        //wps is stored in OpenNebula Cloud Provider
                        case "one":
                            wps = new CloudWpsFactory(context).CreateWpsProcessOfferingForOne(identifierParams[1], identifierParams[2]);
                            break;
                        default:
                            break;
                    }
                }
            }
            if (wps == null) throw new Exception("Unknown identifier");
            return wps;
        }

    }
}

