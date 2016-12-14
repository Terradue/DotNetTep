﻿using System;
using Terradue.Portal;
using Terradue.OpenNebula;
using System.Collections.Generic;
using System.Xml;
using Terradue.Cloud;


/*!
\defgroup CloudWpsFactory WPS Factory
@{

This component enables Cloud Appliance that exposes a WPS service to be exposed as a processing service automatically.

\ingroup Cloud

Using the interface to the cloud controller, it retrieves the VMs that exposes a WPS interface in the cloud.

\xrefitem dep "Dependencies" "Dependencies" uses \ref CoreWPS to analyse and manage the WPS services.

\xrefitem int "Interfaces" "Interfaces" connects \ref OpenNebulaXMLRPC to discover the dynamic WPS providers in the Cloud.



@}
 */

namespace Terradue.Tep {

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

        public void StartDelegate(string username){
            cloudusername = username;
        }

        public void EndDelegate(){
            CloudUser cuser = CloudUser.FromIdAndProvider(context, context.UserId, oneCloud.Id);
            cloudusername = cuser.CloudUsername;
        }

        public CloudWpsFactory(IfyContext context) {
            this.context = context;
        }

        public EntityList<WpsProcessOffering> GetWpsServices (bool canCache, bool fromDb = true, bool fromCloud = true) {

            EntityList<WpsProcessOffering> wpsProcesses = new EntityList<WpsProcessOffering> (context);
            wpsProcesses.Template.Available = true;
            if(fromDb) wpsProcesses.Load ();

            if (fromCloud) {
                List<WpsProvider> wpss = new List<WpsProvider> ();
                try {
                    wpss = GetWPSFromVMs();
                } catch (Exception e) {
                    //we do nothing, we will return the list without any WPS from the cloud
                }

                int workerThreads, completionPortThreads;
                System.Threading.ThreadPool.GetMaxThreads (out workerThreads, out completionPortThreads);

                try {
                    System.Threading.Tasks.Parallel.ForEach (wpss, wps => PerformGetProcessWpsOffering (wpsProcesses, wps, canCache));
                } catch (AggregateException e) {
                    foreach (var e1 in e.InnerExceptions) {
                        log.Error (e1.Message);
                    }
                }

                //foreach (WpsProvider wps in wpss) {
                //    wps.CanCache = canCache;
                //    try {
                //        foreach (WpsProcessOffering process in wps.GetWpsProcessOfferingsFromRemote ()) {
                //            wpsProcesses.Include (process);
                //        }
                //    } catch (Exception e) {
                //        //we do nothing, we just dont add the process
                //    }
                //}
            }
            return wpsProcesses;

        }

        private void PerformGetProcessWpsOffering (EntityList<WpsProcessOffering> wpsProcesses, WpsProvider wps, bool canCache) { 
            wps.CanCache = canCache;
            try {
                foreach (WpsProcessOffering process in wps.GetWpsProcessOfferingsFromRemote ()) {
                    wpsProcesses.Include (process);
                }
            } catch (Exception e) {
                //we do nothing, we just dont add the process
            }
        }

        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public List<WpsProvider> GetWPSFromVMs(){
            if (context.UserId == 0) return new List<WpsProvider>();

            List<WpsProvider> result = new List<WpsProvider>();
            try{
                StartDelegate(context.UserId);
                context.LogDebug(this,string.Format("Get VM Pool for user {0}",cloudusername));
                VM_POOL pool = oneClient.VMGetPoolInfo(-2, -1, -1, 3);
                if(pool != null && pool.VM != null){
                    context.LogDebug(this,string.Format("{0} VM found",pool.VM != null ? pool.VM.Length : 0));
                    foreach (VM vm in pool.VM) {
                        if(vm.USER_TEMPLATE == null) continue;
                        try{
                            XmlNode[] user_template = (XmlNode[])vm.USER_TEMPLATE;
                            foreach(XmlNode nodeUT in user_template){
                                if(nodeUT.Name == "WPS"){
                                    context.LogDebug(this,string.Format("WPS found : {0} - {1}",vm.ID, vm.GNAME));
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
            StartDelegate(oneCloud.AdminUsr);
            VM vm = oneClient.VMGetInfo(Int32.Parse(id));
            EndDelegate();
            return CreateWpsProviderForOne(context, vm);
        }

        /// <summary>
        /// Creates the wps provider from VM (OpenNebula).
        /// </summary>
        /// <returns>The wps provider</returns>
        /// <param name="vm">Virtual machine object.</param>
        public static WpsProvider CreateWpsProviderForOne(IfyContext context, VM vm){
            context.LogDebug (context, "VM id = " + vm.ID);
            context.LogDebug (context, "VM name = " + vm.GNAME);
            WpsProvider wps = new WpsProvider(context);
            wps.Name = vm.NAME;
            wps.Identifier = "one-" + vm.ID;
            wps.Proxy = true;

            wps.Description = vm.UNAME + " from laboratory " + vm.GNAME;
            XmlNode[] template = (XmlNode[])vm.TEMPLATE;
            foreach (XmlNode nodeT in template) {
                context.LogDebug (context, "node name = " + nodeT.Name);
                if (nodeT.Name == "NIC") {
                    wps.BaseUrl = String.Format("http://{0}:8080/wps/WebProcessingService" , nodeT["IP"].InnerText);
                    context.LogDebug (context, "wpsbaseurl = " + wps.BaseUrl);
                    break;
                }
            }
            return wps;
        }

        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public WpsProcessOffering CreateWpsProcessOfferingForOne(string vmId, string processId, bool cancache = true){
            WpsProvider wps = this.CreateWpsProviderForOne(vmId);
            wps.CanCache = cancache;

            foreach (WpsProcessOffering process in wps.GetWpsProcessOfferingsFromRemote()) {
                context.LogDebug (this, "Get process -- " + process.RemoteIdentifier);
                if (process.RemoteIdentifier.Equals (processId)) return process;
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

