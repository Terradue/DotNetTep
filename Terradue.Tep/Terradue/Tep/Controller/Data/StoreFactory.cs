using System;
using System.Collections.Generic;
using Terradue.Artifactory;
using Terradue.Artifactory.Response;
using Terradue.Portal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Flurl.Http.Configuration;
using System.IO;

namespace Terradue.Tep {
    public class StoreFactory {

        public const string PERMISSION_ADMIN = "m";
        public const string PERMISSION_DELETE = "d";
        public const string PERMISSION_DEPLOY = "w";
        public const string PERMISSION_ANNOTATE = "n";
        public const string PERMISSION_READ = "r";

        public IfyContext Context { get; set; }
        private ArtifactoryBaseUrl ArtifactoryBaseUrl { get; set; }

        static System.Collections.Specialized.NameValueCollection AppSettings = System.Configuration.ConfigurationManager.AppSettings;
        public static string storeBaseUrl = AppSettings["DataGatewayBaseUrl"];
        public static string storeApikey = AppSettings["DataGatewayApikey"];

        public NewtonsoftJsonSerializer Serializer { get; protected set; }

        public StoreFactory(IfyContext context, string userApikey) {
            this.Context = context;
            ArtifactoryBaseUrl = new ArtifactoryBaseUrl(storeBaseUrl, userApikey);
            Serializer = ArtifactoryBaseUrl.Serializer;
        }

        /***************************************************************************************************************************************/

        #region User

        /// <summary>
        /// Gets the user info.
        /// </summary>
        /// <returns>The user info.</returns>
        /// <param name="username">Username.</param>
        public SecurityUser GetUserInfo(string username) {
            return ArtifactoryBaseUrl.Security().GetUserDetails(username);
        }

        /// <summary>
        /// Deletes the user.
        /// </summary>
        /// <param name="username">Username.</param>
        public void DeleteUser(string username) {
            ArtifactoryBaseUrl.Security().DeleteUser(username);
        }

        #endregion

        #region Repository

        /// <summary>
        /// Creates the local repository.
        /// </summary>
        /// <param name="repo">Repo.</param>
        /// <param name="removeOld">If set to <c>true</c> remove old.</param>
        public void CreateLocalRepository(string repo, bool removeOld = false) {
            if (removeOld || !RepositoryExists(repo)) {
                var config = new LocalRepositoryConfiguration {
                    Key = repo,
                    packageType = RepositoryConfiguration.PackageType.generic
                };

                ArtifactoryBaseUrl.Repositories().CreateRepository(config);
            }
        }

        /// <summary>
        /// Repositories the exists.
        /// </summary>
        /// <returns><c>true</c>, if exists was repositoryed, <c>false</c> otherwise.</returns>
        /// <param name="repo">Repo.</param>
        public bool RepositoryExists(string repo) {
            try {
                var repository = ArtifactoryBaseUrl.Repositories().RepositoryConfiguration(repo);
                if (repository != null) return true;
            } catch (Exception) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the storage info.
        /// </summary>
        /// <param name="repo">Repo.</param>
        public RepositoriesSummary GetStorageInfo(string repo) {
            if (string.IsNullOrEmpty(repo)) throw new Exception("Invalid storage name : " + repo);
            var info = ArtifactoryBaseUrl.Storage().StorageInfo();
            foreach (var storage in info.repositoriesSummaryList) {
                if (repo.Equals(storage.repoKey)) {
                    return storage;
                }
            }
            return null;
        }

        #endregion

        /***************************************************************************************************************************************/

        #region Group

        /// <summary>
        /// Creates the group.
        /// </summary>
        /// <param name="group">Group.</param>
        public void CreateGroup(string group, string dn) {

            SecurityGroup config = new SecurityGroup {
                Name = group,
                Description = "Group synchronized from LDAP",
                Realm = "ldap",
                AutoJoin = false,
                RealmAttributes = string.Format("ldapGroupName={0};groupsStrategy=STATIC;groupDn={1}", group, dn)
            };

            ArtifactoryBaseUrl.Security().CreateOrReplaceGroup(config);
        }

        /// <summary>
        /// Gets the groups for user.
        /// </summary>
        /// <returns>The groups for user.</returns>
        /// <param name="username">Username.</param>
        public List<string> GetGroupsForUser(string username) {
            SecurityUser userinfo = GetUserInfo(username);
            return userinfo.Groups;
        }

        /// <summary>
        /// Gets the groups.
        /// </summary>
        /// <returns>The groups.</returns>
        public List<string> GetGroups() {
            List<string> groups = new List<string>();
            var artifactoryGroups = ArtifactoryBaseUrl.Security().GetGroups();
            foreach (var g in artifactoryGroups)
                groups.Add(g.Name);
            return groups;
        }

        #endregion

        /***************************************************************************************************************************************/

        #region Permissions

        /// <summary>
        /// Creates the permission for group on repo.
        /// </summary>
        /// <param name="group">Group.</param>
        /// <param name="repo">Repo.</param>
        public void CreatePermissionForGroupOnRepo(string group, string repo) {

            PermissionTarget permissionTarget = new PermissionTarget {
                Name = group,
                Repositories = new List<string> { repo }
            };

            Permissions permissions = new Permissions();
            permissions.Add(PERMISSION_DELETE);
            permissions.Add(PERMISSION_READ);
            permissions.Add(PERMISSION_DEPLOY);
            permissions.Add(PERMISSION_ANNOTATE);
            permissionTarget.Principals.Groups.Clear();
            permissionTarget.Principals.Groups.Add(group, permissions);

            ArtifactoryBaseUrl.Security().CreateOrReplacePermissionTarget(permissionTarget);
        }

        #endregion

        /***************************************************************************************************************************************/

        #region APIKEY

        /// <summary>
        /// Gets the API key.
        /// </summary>
        /// <returns>The API key.</returns>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        public string GetApiKey(string username, string password) {
            var userArtifactoryBaseUrl = new ArtifactoryBaseUrl(this.Context.GetConfigValue("artifactory-APIurl"), username, password);
            var apikey = userArtifactoryBaseUrl.Security().GetApiKey();
            return apikey.apiKey;
        }

        /// <summary>
        /// Creates the API key.
        /// </summary>
        /// <returns>The API key.</returns>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        public string CreateApiKey(string username, string password) {
            var userArtifactoryBaseUrl = new ArtifactoryBaseUrl(this.Context.GetConfigValue("artifactory-APIurl"), username, password);
            SecurityApiKey key = null;
            try {
                key = userArtifactoryBaseUrl.Security().GetApiKey();
                if (key != null) userArtifactoryBaseUrl.Security().RevokeApiKey();
            } catch (Exception) { }

            try {
                key = userArtifactoryBaseUrl.Security().CreateApiKey();
            } catch (Exception e) {
                this.Context.LogError(this, e.Message + "-" + e.StackTrace);
            }

            if (key != null) return key.apiKey;
            else return null;
        }

        /// <summary>
        /// Revokes the API key.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        public void RevokeApiKey(string username, string password) {
            var userArtifactoryBaseUrl = new ArtifactoryBaseUrl(this.Context.GetConfigValue("artifactory-APIurl"), username, password);
            userArtifactoryBaseUrl.Security().RevokeApiKey();
        }

        #endregion

        #region TOKEN

        /// <summary>
        /// Generates the token.
        /// </summary>
        /// <returns>The token.</returns>
        /// <param name="username">Username.</param>
        /// <param name="domains">Domains.</param>
        /// <param name="expires_in">Expires in.</param>
        public Token GenerateToken(string username, List<string> domains, int expires_in = 3600) {

            var scope = string.Format("member-of-groups:{0}", string.Join(",", domains));
            TokenRequest config = new TokenRequest {
                username = username,
                scope = scope,
                expires_in = expires_in
            };

            var token = ArtifactoryBaseUrl.Security().GenerateToken(config);
            return token;
        }

        #endregion

        #region STORAGE

        /// <summary>
        /// Gets the item info.
        /// </summary>
        /// <returns>The item (folder or file) info.</returns>
        /// <param name="repoKey">Repo key.</param>
        /// <param name="path">Path.</param>
        public Artifactory.Response.FileInfo GetItemInfo(string repoKey, string path) {
            if (Context.GetConfigBooleanValue("artifactory_repo_restriction_deploy")) CheckRepoRestriction(repoKey);
            return ArtifactoryBaseUrl.Storage().FileInfo(repoKey, path);
        }

        public RepositoryInfoList GetRepositoriesToDeploy() {
            return ArtifactoryBaseUrl.Repositories().GetRepositoriesToDeploy();
        }

        public void DeleteFile(string repoKey, string path) {
            ArtifactoryBaseUrl.Storage().DeleteItem(repoKey, path);
        }

        public void UploadFile(string repoKey, string path, string filename) {
            TextReader reader = File.OpenText(filename);
            ArtifactoryBaseUrl.Deploy(repoKey, path, reader);
        }

        public FolderInfo CreateFolder(string repoKey, string path) {
            return ArtifactoryBaseUrl.Storage().CreateFolder(repoKey, path);
        }

        public MessageContainer MoveItem(string srcRepoKey, string srcFilePath, string targetRepoKey, string targetFilePath, int dry) {
            return ArtifactoryBaseUrl.Storage().MoveItem(srcRepoKey, srcFilePath, targetRepoKey, targetFilePath, dry);
        }

        public Stream DownloadItem(string repoKey, string path) {
            return ArtifactoryBaseUrl.Download(repoKey, path);
        }

        private void CheckRepoRestriction(string repoKey) {
            RepositoryInfoList repos = GetRepositoriesToDeploy();
            bool canAccessRepo = false;
            if (repos != null && repos.RepoTypesList != null) {
                foreach (var repo in repos.RepoTypesList) {
                    if (repoKey == repo.RepoKey) canAccessRepo = true;
                }
            }
            if (!canAccessRepo) throw new Exception("The requested path is not valid");
        }

        #endregion
        /***************************************************************************************************************************************/
    }
}
