using System;
using NUnit.Framework;
using OpenGis.Wps;
using Terradue.Portal;

namespace Terradue.Tep.Test {
	[TestFixture]
	public class WpsJobGetStatusTest : BaseTest {

		[TestFixtureSetUp]
        public override void FixtureSetup() {
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";         
			context.AccessLevel = EntityAccessLevel.Administrator;
        }

        private WpsProvider CreateProvider(string identifier, string name, string url, bool proxy) {
            WpsProvider provider;
            provider = new WpsProvider(context);
            provider.Identifier = identifier;
            provider.Name = name;
            provider.Description = name;
            provider.BaseUrl = url;
            provider.Proxy = proxy;
            try {
                provider.Store();
            } catch (Exception e) {
                throw e;
            }
            return provider;
        }

        private WpsProcessOffering CreateProcess(WpsProvider provider, string identifier, string name) {
            WpsProcessOffering process = new WpsProcessOffering(context);
            process.Name = name;
            process.Description = name;
            process.RemoteIdentifier = identifier;
            process.Identifier = Guid.NewGuid().ToString();
            process.Url = provider.BaseUrl;
            process.Version = "1.0.0";
            process.Provider = provider;
            return process;
        }



		[Test]
		public void LoadWpsJobStatus() {
			WpsProvider provider = CreateProvider("planetek", "planetek", "http://urban-tep.planetek.it/wps/WebProcessingService", true);
			provider.Store();
            WpsProcessOffering process = CreateProcess(provider, "it.planetek.wps.extension.Processor", "Title of the processor");
			process.Store();
			WpsJob job = new WpsJob(context);
			job.WpsId = process.Identifier;
			job.StatusLocation = "http://urban-tep.planetek.it/wps/RetrieveResultServlet?id=72ed982a-8522-4c02-bb71-77f4c22a7808";

			var jobresponse = job.GetStatusLocationContent();
			var execResponse = jobresponse as ExecuteResponse;
			job.UpdateStatusFromExecuteResponse(execResponse);
            
            //get job recast response
            execResponse = ProductionResultHelper.GetWpsjobRecastResponse(context, job, execResponse);
			Assert.True(true);
		}
	}
}
    
