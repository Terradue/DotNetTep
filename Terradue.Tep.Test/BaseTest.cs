﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Terradue.Portal;

namespace Terradue.Tep.Test {

    public class BaseTest {

        protected IfyContext context;
        private string connectionString;

        protected string DatabaseName { get; set; }
        protected string BaseDirectory { get; set; }

        public string GetConnectionString() {
            string result = "Server=localhost; Port=3306; User Id=root; Database=DotNetTepTest; Max Pool Size=200";
            bool replaceDatabaseName = (DatabaseName != null);
            Match match = Regex.Match(result, "Database=([^;]+)");
            if (replaceDatabaseName) {
                if (match.Success) result = result.Replace(match.Value, "Database=" + DatabaseName);
                else result += "; Database=" + DatabaseName;
            } else {
                if (match.Success) DatabaseName = match.Groups [1].Value;
                else throw new Exception("No database name specified");
            }
            return result;
        }

        [OneTimeSetUp]
        public virtual void FixtureSetup() {
            connectionString = GetConnectionString();
            if (BaseDirectory == null) BaseDirectory = Directory.GetCurrentDirectory() + "/../database";

            AdminTool adminTool = new AdminTool(DataDefinitionMode.Create, BaseDirectory, null, connectionString);

            try {
                adminTool.Process();
                context = IfyContext.GetLocalContext(connectionString, false);
                context.Open();
                context.LoadConfiguration();
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
                throw;
            }

            System.Configuration.ConfigurationManager.AppSettings["DataGatewayBaseUrl"] = "https://store.terradue.com";
            System.Configuration.ConfigurationManager.AppSettings["DataGatewayShareUrl"] = "https://recast.terradue.com/t2api/share";
            System.Configuration.ConfigurationManager.AppSettings["DataGatewaySecretKey"] = "abcdefg";
            System.Configuration.ConfigurationManager.AppSettings["RecastBaseUrl"] = "https://recast.terradue.com";
            System.Configuration.ConfigurationManager.AppSettings["CatalogBaseUrl"] = "https://catalog.terradue.com";

        }

        [TearDown]
        public virtual void FixtureTearDown() {

            //context.Execute(String.Format("DROP DATABASE {0};", DatabaseName));
            context.Close();
        }
    }
}

