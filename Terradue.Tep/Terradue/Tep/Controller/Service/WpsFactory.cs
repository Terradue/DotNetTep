using System;
using OpenGis.Wps;
using Terradue.Portal;
using System.Collections.Generic;
using System.Net;
using System.IO;
using ServiceStack.Common.Web;
using System.Xml.Serialization;
using System.Runtime.Caching;


/*!
\defgroup TepService Service
@{

This component controls the processing services registered in the platform.

WPS discovery is enabled by loading the services using the \ref CoreWPS component.

\xrefitem dep "Dependencies" "Dependencies" uses \ref CloudWpsFactory to discover the dynamic WPS providers in the Cloud.

\xrefitem dep "Dependencies" "Dependencies" uses \ref CoreWPS to analyse and manage the WPS services.

\ingroup Tep

@}

*/

namespace Terradue.Tep {
    public class WpsFactory {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IfyContext context { get; set; }

        public static XmlSerializer ExceptionReportSerializer = new XmlSerializer(typeof(OpenGis.Wps.ExceptionReport));
        public static XmlSerializer ExecuteResponseSerializer = new XmlSerializer(typeof(OpenGis.Wps.ExecuteResponse));

        public WpsFactory(IfyContext context) {
            this.context = context;
        }

        //****************************************************************************************
        // GetCapabilities
        //****************************************************************************************

        public WPSCapabilitiesType WpsGetCapabilities(){
            context.LogDebug(this,string.Format("WPS GetCapabilities requested"));

            //create GetCapabilities response
            WPSCapabilitiesType getCapabilities = CreateGetCapabilititesTemplate(context.BaseUrl + "/wps/WebProcessingService");


            //load providers from DB
            EntityList<WpsProvider> wpsList = new EntityList<WpsProvider>(context);
            wpsList.Load();

            //for each provider, if proxied, get processOfferings and add it in the response
            foreach (WpsProvider process in wpsList) {
                if (process.Proxy) {
                    //get processings from db
                    List<ProcessBriefType> processOfferings = process.GetProcessBriefTypes();

                    foreach (ProcessBriefType processOff in processOfferings) {
                        try{
                            getCapabilities.ProcessOfferings.Process.Add(processOff);
                            context.LogDebug(this,string.Format("WPS GetCapabilities - " + processOff.Title.Value));
                        }catch(Exception){}
                    }
                }
            }
                
            //load providers from Cloud Provider
            CloudWpsFactory wpsFinder = new CloudWpsFactory(context);
            try{
                foreach (WpsProvider prov in wpsFinder.GetWPSFromVMs()) {
                    try{
                        WPSCapabilitiesType capa = prov.GetWPSCapabilities();
                        foreach (var oneProcess in capa.ProcessOfferings.Process) {
                            oneProcess.Identifier.Value = prov.Identifier + "-" + oneProcess.Identifier.Value;
                            getCapabilities.ProcessOfferings.Process.Add(oneProcess);
                            context.LogDebug(this,string.Format("WPS GetCapabilities - " + oneProcess.Title.Value));
                        }
                    }catch(Exception){}
                }
            }catch(Exception){}

            return getCapabilities;
        }

        /// <summary>
        /// Creates the get capabilitites template.
        /// </summary>
        /// <returns>The get capabilitites template.</returns>
        /// <param name="baseUrl">Base URL.</param>
        public static OpenGis.Wps.WPSCapabilitiesType CreateGetCapabilititesTemplate(string baseUrl) {
            UriBuilder baseUri = new UriBuilder(baseUrl);
            OpenGis.Wps.WPSCapabilitiesType capabilitites = new OpenGis.Wps.WPSCapabilitiesType();

            capabilitites.ServiceIdentification = new OpenGis.Wps.ServiceIdentification();
            capabilitites.ServiceIdentification.Title = new List<OpenGis.Wps.LanguageStringType>{ new OpenGis.Wps.LanguageStringType{ Value = "Tep WPS" } };
            capabilitites.ServiceIdentification.Abstract = new List<OpenGis.Wps.LanguageStringType>{ new OpenGis.Wps.LanguageStringType{ Value = "Proxy WPS of the TEP platform" } };
            capabilitites.ServiceIdentification.Keywords = new List<OpenGis.Wps.KeywordsType>();
            OpenGis.Wps.KeywordsType kw1 = new OpenGis.Wps.KeywordsType{ Keyword = new List<OpenGis.Wps.LanguageStringType>{ new OpenGis.Wps.LanguageStringType{ Value = "WPS" } } };
            List<OpenGis.Wps.LanguageStringType> listKeywords = new List<OpenGis.Wps.LanguageStringType>();
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "WPS" });
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "TEP" });
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "geospatial" });
            listKeywords.Add(new OpenGis.Wps.LanguageStringType{ Value = "geoprocessing" });
            capabilitites.ServiceIdentification.Keywords.Add(new OpenGis.Wps.KeywordsType{ Keyword = listKeywords });
            capabilitites.ServiceIdentification.ServiceType = new OpenGis.Wps.CodeType{ Value = "WPS" };
            capabilitites.ServiceIdentification.ServiceTypeVersion = new List<string>{ "1.0.0" };
            capabilitites.ServiceIdentification.Fees = "None";
            capabilitites.ServiceIdentification.AccessConstraints = new List<string>{ "NONE" };

            capabilitites.ServiceProvider = new OpenGis.Wps.ServiceProvider();
            capabilitites.ServiceProvider.ProviderName = "Tep";
            capabilitites.ServiceProvider.ProviderSite = new OpenGis.Wps.OnlineResourceType{ href = "https://tep.eo.esa.int/" };

            capabilitites.OperationsMetadata = new OpenGis.Wps.OperationsMetadata();
            capabilitites.OperationsMetadata.Operation = new List<OpenGis.Wps.Operation>();
            //Add GetCapabilities OperationMetadata
            OpenGis.Wps.Operation getCapabilitiesOperation = new OpenGis.Wps.Operation();
            getCapabilitiesOperation.name = "GetCapabilities";
            getCapabilitiesOperation.DCP = new List<OpenGis.Wps.DCP>();
            OpenGis.Wps.DCP dcpGetCap = new OpenGis.Wps.DCP();
            dcpGetCap.Item = new OpenGis.Wps.HTTP();
            dcpGetCap.Item.Items = new List<OpenGis.Wps.RequestMethodType>();
            OpenGis.Wps.RequestMethodType getGetCapaDcp = new OpenGis.Wps.GetRequestMethodType();
            getGetCapaDcp.href = baseUrl;
            dcpGetCap.Item.Items.Add(getGetCapaDcp);
            getCapabilitiesOperation.DCP.Add(dcpGetCap);
            capabilitites.OperationsMetadata.Operation.Add(getCapabilitiesOperation);
            //Add DescribeProcess OperationMetadata
            OpenGis.Wps.Operation describeProcessOperation = new OpenGis.Wps.Operation();
            describeProcessOperation.name = "DescribeProcess";
            describeProcessOperation.DCP = new List<OpenGis.Wps.DCP>();
            OpenGis.Wps.DCP dcpDescrPr = new OpenGis.Wps.DCP();
            dcpDescrPr.Item = new OpenGis.Wps.HTTP();
            dcpDescrPr.Item.Items = new List<OpenGis.Wps.RequestMethodType>();
            OpenGis.Wps.RequestMethodType getDescrPrDcp = new OpenGis.Wps.GetRequestMethodType();
            getDescrPrDcp.href = baseUrl;
            dcpDescrPr.Item.Items.Add(getDescrPrDcp);
            describeProcessOperation.DCP.Add(dcpDescrPr);
            capabilitites.OperationsMetadata.Operation.Add(describeProcessOperation);
            //Add Execute OperationMetadata
            OpenGis.Wps.Operation executeOperation = new OpenGis.Wps.Operation();
            executeOperation.name = "Execute";
            executeOperation.DCP = new List<OpenGis.Wps.DCP>();
            OpenGis.Wps.DCP dcpExec = new OpenGis.Wps.DCP();
            dcpExec.Item = new OpenGis.Wps.HTTP();
            dcpExec.Item.Items = new List<OpenGis.Wps.RequestMethodType>();
            OpenGis.Wps.RequestMethodType getExecDcp = new OpenGis.Wps.GetRequestMethodType();
            getExecDcp.href = baseUrl;
            dcpExec.Item.Items.Add(getExecDcp);
            OpenGis.Wps.RequestMethodType postExecDcp = new OpenGis.Wps.PostRequestMethodType();
            baseUri.Query = "";
            postExecDcp.href = baseUri.Uri.AbsoluteUri;
            dcpExec.Item.Items.Add(postExecDcp);
            executeOperation.DCP.Add(dcpExec);
            capabilitites.OperationsMetadata.Operation.Add(executeOperation);

            capabilitites.ProcessOfferings = new OpenGis.Wps.ProcessOfferings();
            capabilitites.ProcessOfferings.Process = new List<OpenGis.Wps.ProcessBriefType>();

            capabilitites.Languages = new OpenGis.Wps.Languages();
            capabilitites.Languages.Default = new OpenGis.Wps.LanguagesDefault{ Language = "en-US" };

            return capabilitites;
        }



        //****************************************************************************************
        // DescribeProcess
        //****************************************************************************************

        public static ProcessDescriptions DescribeProcessCleanup(IfyContext context, ProcessDescriptions input){
			foreach (var desc in input.ProcessDescription) {
                var newinputs = new List<InputDescriptionType>();

                //add inputs
                var newinput = new InputDescriptionType();
                newinput.Identifier = new CodeType { Value = "_T2InternalJobTitle" };
                newinput.Title = new LanguageStringType { Value = "Job title" };
                newinput.Abstract = new LanguageStringType { Value = "Wps job title" };
                newinput.minOccurs = "1";
                newinput.maxOccurs = "1";
                newinput.LiteralData = new LiteralInputType {
                    DataType = new DomainMetadataType { Value = "string " },
                    DefaultValue = desc.Title != null ? desc.Title.Value : ""
                };
                newinputs.Add(newinput);

                foreach (var data in desc.DataInputs) {
                    if (data.LiteralData != null) {
                        if (data.LiteralData.AllowedValues != null && data.LiteralData.AllowedValues.Count == 0) {
                            data.LiteralData.AllowedValues = null;
                        }
                    }
                    //remove inputs
                    if (data.Identifier != null) {
                        switch (data.Identifier.Value) {
                            case "_T2Username":
                            case "_T2UserEmail":
                            case "_T2ApiKey":
                            case "_T2JobInfoFeed":
                            case "_T2ResultsAnalysis":
                            case "_T2InternalJobTitle":
                                break;
                            case "index":
                            case "repoKey":
                                if (data.LiteralData != null && data.LiteralData.DefaultValue != null && context.UserId != 0){
                                    var user = UserTep.FromId(context, context.UserId);
									data.LiteralData.DefaultValue = data.LiteralData.DefaultValue.Replace("${USERNAME}", user.Username);
									data.LiteralData.DefaultValue = data.LiteralData.DefaultValue.Replace("${T2USERNAME}", user.TerradueCloudUsername);
                                }
                                newinputs.Add(data);
                                break;
                            default:
                                newinputs.Add(data);
                                break;
                        }
                    }
                }

                desc.DataInputs = newinputs;
            }
            return input;
        }

		public static bool DescribeProcessHasField(ProcessDescriptions input, string field) {
			foreach (var desc in input.ProcessDescription) {
				foreach (var data in desc.DataInputs) {
                    if (data.Identifier != null && data.Identifier.Value == field) 
                        return true;
				}
			}
            return false;
		}

        //****************************************************************************************
        // Execute
        //****************************************************************************************


    }
}

