using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.Text;
using Terradue.Portal;

namespace Terradue.Tep {
    public class GeoserverFactory {

        static System.Collections.Specialized.NameValueCollection AppSettings = System.Configuration.ConfigurationManager.AppSettings;
        private static string geoserverBaseUrl = AppSettings["GeoserverBaseUrl"];
        private static string geoserverUsername = AppSettings["GeoserverUsername"];
        private static string geoserverPwd = AppSettings["GeoserverSecret"];

        public GeoserverFactory() {}

        public static GeoserverStylesResponse GetGlobalStyles() {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("{0}/rest/styles.json", geoserverBaseUrl));
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Credentials = new NetworkCredential(geoserverUsername, geoserverPwd);
            request.Proxy = null;

            GeoserverStylesResponse response = null;

            try {
                using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        response = JsonSerializer.DeserializeFromString<GeoserverStylesResponse>(result);
                    }
                }
            } catch (Exception e) {
                throw e;
            }

            return response;
        }

        public static GeoserverStylesResponse GetStylesForLayer(string layer) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("{0}/rest/layers/{1}/styles.json", geoserverBaseUrl, layer));
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Credentials = new NetworkCredential(geoserverUsername, geoserverPwd);
            request.Proxy = null;

            GeoserverStylesResponse response = null;

            try {
                using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        response = JsonSerializer.DeserializeFromString<GeoserverStylesResponse>(result);
                    }
                }
            } catch (Exception e) {
                throw e;
            }

            return response;
        }

        public static GeoserverStylesResponse GetStylesForWorkspace(string workspace) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("{0}/rest/workspaces/{1}/styles.json", geoserverBaseUrl, workspace));
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Credentials = new NetworkCredential(geoserverUsername, geoserverPwd);
            request.Proxy = null;

            GeoserverStylesResponse response = null;

            try {
                using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        response = JsonSerializer.DeserializeFromString<GeoserverStylesResponse>(result);
                    }
                }
            } catch (Exception e) {
                throw e;
            }

            return response;
        }
    }

    [DataContract]
    public class GeoserverStylesResponse {
        [DataMember]
        public GeoserverStyles Styles { get; set; }
    }

    [DataContract]
    public class GeoserverStyles {
        [DataMember]
        public GeoserverStyle[] Style { get; set; }
    }

    [DataContract]
    public class GeoserverStyle {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public Uri Href { get; set; }
    }
}


/*
 * {
   styles: 
   {
      style:[
         {
            name: "raster",
            href: "http://95.216.170.118:8080/geoserver/rest/layers/ceras:GEO_DPHASE2/styles/raster.json"
         },
         {
            name: "dem",
            href: "http://95.216.170.118:8080/geoserver/rest/layers/ceras:GEO_DPHASE2/styles/dem.json"
         },
         {
            name: "test",
            href: "http://95.216.170.118:8080/geoserver/rest/layers/ceras:GEO_DPHASE2/styles/test.json"
         }
      ]
   }
}
 */
