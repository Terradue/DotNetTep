using System;
using System.Collections.Generic;
using System.Data;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using Terradue.OpenNebula;
using Terradue.Portal;
using Terradue.Tep.WebServer;
using Terradue.WebService.Model;

namespace Terradue.Tep.WebServer.Services {
    [Api("Tep Terradue webserver")]
    [Restrict(EndpointAttributes.InSecure | EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Json,
              EndpointAttributes.Secure | EndpointAttributes.External | EndpointAttributes.Json)]
    public class ConfigServiceTep : ServiceStack.ServiceInterface.Service {

        public object Get(GetConfig request) {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            var context = TepWebContext.GetWebContext(PagePrivileges.EverybodyView);
            try {
                context.Open();

                result.Add(new KeyValuePair<string, string>("geobrowserDateMin",context.GetConfigValue("geobrowser-date-min")));
                result.Add(new KeyValuePair<string, string>("geobrowserDateMax",context.GetConfigValue("geobrowser-date-max")));
                result.Add(new KeyValuePair<string, string>("Github-client-id",context.GetConfigValue("Github-client-id")));
                result.Add(new KeyValuePair<string, string>("enableAccounting", context.GetConfigValue("enableAccounting")));
                    
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Get(GetDBConfigRequestTep request) {
            List<WebDBConfig> result = new List<WebDBConfig>();

            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();

                string sql = "SELECT name,value,type FROM config;";
                IDbConnection dbConnection = context.GetDbConnection();
                IDataReader reader = context.GetQueryResult(sql, dbConnection);

                while (reader.Read()) {
                    try {
                        result.Add(new WebDBConfig {
                            Name = reader.GetString(0),
                            Value = (reader.GetValue(1) == System.DBNull.Value ? "" : reader.GetString(1)),
                            Type = reader.GetString(2)
                        });
                    } catch (Exception e) {
                        context.LogError(this, e.Message);
                    }
                }
                context.CloseQueryResult(reader, dbConnection);
                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return result;
        }

        public object Post(CreateDBConfigRequestTep request) { 
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();

                if (string.IsNullOrEmpty(request.Name)) throw new Exception("Invalid config name");

                string sql = string.Format("INSERT INTO config (name, value, type) VALUES ('{0}','{1}','{2}');", request.Name, request.Value, request.Type);
                context.Execute(sql);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Put(UpdateDBConfigRequestTep request) { 
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();

                if (string.IsNullOrEmpty(request.Name)) throw new Exception("Invalid config name");

                string sql = string.Format("UPDATE config SET value='{0}', type='{1}' WHERE name='{2}';", request.Value, request.Type, request.Name);
                context.Execute(sql);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }

        public object Delete(DeleteDBConfigRequestTep request) {
            var context = TepWebContext.GetWebContext(PagePrivileges.AdminOnly);
            try {
                context.Open();

                if (string.IsNullOrEmpty(request.Name)) throw new Exception("Invalid config name");

                string sql = string.Format("DELETE FROM config WHERE name='{0}';", request.Name);
                context.Execute(sql);

                context.Close();
            } catch (Exception e) {
                context.Close();
                throw e;
            }
            return new WebResponseBool(true);
        }
     }
}