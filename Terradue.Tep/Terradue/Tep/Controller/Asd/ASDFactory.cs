using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using OpenGis.Wps;
using ServiceStack.Text;
using Terradue.Portal;
using Terradue.Portal.Urf;

namespace Terradue.Tep {
    public class ASDFactory {
        
        public ASDFactory() {}

        /// <summary>
        /// Get the user ASDs
        /// </summary>
        /// <param name="context"></param>
        /// <param name="usr"></param>
        /// <returns></returns>
        public static List<List<UrfTep>> GetUserASDs(IfyContext context, UserTep usr, bool sync = true) {
            var url = string.Format("{0}?token={1}&username={2}&request=urf",
                                context.GetConfigValue("t2portal-usrinfo-endpoint"),
                                context.GetConfigValue("t2portal-safe-token"),
                                usr.TerradueCloudUsername);

            HttpWebRequest t2request = (HttpWebRequest)WebRequest.Create(url);
            t2request.Method = "GET";
            t2request.ContentType = "application/json";
            t2request.Accept = "application/json";
            t2request.Proxy = null;

            var factory = new ASDTransactionFactory(context);
            
            var urfs = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(t2request.BeginGetResponse,
                                                                       t2request.EndGetResponse,
                                                                       null)
            .ContinueWith(task =>
            {
                try{
                    var httpResponse = (HttpWebResponse) task.Result;
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();                    
                        return JsonSerializer.DeserializeFromString<List<List<UrfTep>>>(result);                    
                    }
                }catch(Exception e){
                    throw e;
                }
            }).ConfigureAwait(false).GetAwaiter().GetResult();

            if(sync && urfs != null){
                foreach (var urf in urfs) {                    
                    if(urf.Count > 0){                        
                        var asd = urf[0];
                        //check if urf already stored in DB
                        var dburfs = new EntityList<ASD>(context);
                        dburfs.SetFilter("Identifier", asd.UrfInformation.Identifier);
                        dburfs.Load();
                        var items = dburfs.GetItemsAsList();
                        if(items.Count == 0){
                            //not stored in DB
                            var asdToStore = ASD.FromURF(context, asd);
                            asdToStore.Store();
                            asdToStore.AddPermissions(asd);
                        } else {                            
                            var dbasd = items[0];
                            //check if credit updated
                            if(asd.UrfInformation.Credit != dbasd.CreditTotal){
                                dbasd.CreditTotal = asd.UrfInformation.Credit;
                                dbasd.Store();
                            }
                            //check if status updated
                            if(asd.UrfInformation.Status != dbasd.Status){
                                dbasd.Status = asd.UrfInformation.Status;
                                dbasd.Store();
                            }
                            //check if overspending allowed updated
                            if(asd.UrfInformation.Overspending != dbasd.Overspending){
                                dbasd.Overspending = asd.UrfInformation.Overspending;
                                dbasd.Store();
                            }
                            //check if users changed
                            dbasd.SyncUsers(asd);

                            asd.UrfCreditInformation.Credit = dbasd.CreditTotal;
                            asd.UrfCreditInformation.CreditRemaining = dbasd.CreditRemaining;
                            asd.UrfCreditInformation.Transactions = factory.GetServiceTransactionsForASD(dbasd.Id);
                        }
                    }
                }
            }
            return urfs;
        }

        public static List<ASDBasic> GetActiveASDsFromUseremails(IfyContext context, List<string> useremails) {
            var url = string.Format("{0}?token={1}&status={2}",
                                context.GetConfigValue("t2portal-urf-posturl"),
                                context.GetConfigValue("t2portal-safe-token"),
                                (int)UrfStatus.Activated);

            HttpWebRequest t2request = (HttpWebRequest)WebRequest.Create(url);
            t2request.Method = "GET";
            t2request.ContentType = "application/json";
            t2request.Accept = "application/json";
            t2request.Proxy = null;
            
            var urfs = System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(t2request.BeginGetResponse,
                                                                       t2request.EndGetResponse,
                                                                       null)
            .ContinueWith(task =>
            {
                try{
                    var httpResponse = (HttpWebResponse) task.Result;
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();                    
                        return JsonSerializer.DeserializeFromString<List<ASDBasic>>(result);                    
                    }
                }catch(Exception e){
                    throw e;
                }
            }).ConfigureAwait(false).GetAwaiter().GetResult();

            return urfs;
        }

    }
}
