using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Terradue.Artifactory;
using Terradue.Artifactory.Response;

namespace Terradue.Tep {
    [JsonObject(MemberSerialization.OptIn)]
    public class ShareRequest {

        static System.Collections.Specialized.NameValueCollection AppSettings = System.Configuration.ConfigurationManager.AppSettings;

        protected static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected static Mutex _lock = new Mutex();

        private ArtifactoryBaseUrl storeBaseUrl;

        private ShareRequest() { }

        public ShareRequest(ArtifactoryBaseUrl baseUrl) {
            storeBaseUrl = baseUrl;
        }

        #region JSON attributes

        [JsonProperty]
        public string Origin;

        [JsonProperty]
        public string Type;

        [JsonProperty]
        public string Visibility;

        [JsonProperty]
        public string Identifier;

        [JsonProperty]
        public System.Collections.Generic.List<Community> Communities;

        [JsonProperty]
        public System.Collections.Generic.List<User> Users;

        #endregion


        internal static ShareRequest LoadFromJsonString(string payload, ArtifactoryBaseUrl storeBaseUrl) {
            JsonSerializer serializer = new JsonSerializer();
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializer.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;

            ShareRequest shareRequest = (ShareRequest)serializer.Deserialize(new StringReader(payload), typeof(ShareRequest));
            shareRequest.storeBaseUrl = storeBaseUrl;

            return shareRequest;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken) {
            var task = new Task<HttpResponseMessage>(() => ApplyShare());
            task.Start();
            return task;
        }

        private HttpResponseMessage ApplyShare() {

            // Synchronous part
            try {
                // transaction start
                _lock.WaitOne();

                // 1/ Validate the request
                ValidateRequest();

                // 2/ Synchronize Communities And Users
                SynchronizeCommunitiesAndUsers();

                // 3/ Apply the share
                ApplySharing();

                // Response success
                var response = new HttpResponseMessage() {
                    Content = new StringContent("OK")
                };

                return response;
            } catch (AggregateException ae) {
                foreach (Exception ex in ae.InnerExceptions) {
                    log.ErrorFormat("Error handling share : {0}", ex.Message);
                    log.Debug(ex.StackTrace);
                }
                throw ae;
            } catch (Exception e) {
                log.ErrorFormat("Error handling share : {0}", e.Message);
                log.Debug(e.StackTrace);
                throw e;
            } finally {
                _lock.ReleaseMutex();
            }
        }

        private void ApplySharing() {
            // 1/ find the repo and path to share from the identifier
            string repo = null;
            string path = null;
            FindRepoAndPathFromIdentifier(out repo, out path);

            switch (Visibility) {
                case "private":
                    DeleteAllPermissions(repo, path);
                    break;
                case "restricted":
                    // 2/ Create or Update the virtual repo for the community
                    CreateOrUpdateCommunityVirtualRepo(repo, path);
                    // 3/ Create or update the permissions
                    CreateOrUpdateRestrictedPermissions(repo, path);
                    break;
                case "public":
                    // 2/ Make public
                    CreateOrUpdatePublicPermissions(repo, path);
                    break;
            }

        }

        private void DeleteAllPermissions(string repo, string path) {
            string prefix = FindPrefixFromOrigin();

            string permissionTargetKey = string.Format("shr-{0}-{1}-{2}.pub", prefix, repo.Replace(".", "").Replace(" ", ""), path.GetHashCode());

            storeBaseUrl.Security().DeletePermissionTarget(permissionTargetKey);

            permissionTargetKey = string.Format("shr-{0}-{1}-{2}.reader", prefix, repo.Replace(".", "").Replace(" ", ""), path.GetHashCode());

            storeBaseUrl.Security().DeletePermissionTarget(permissionTargetKey);
        }

        private void CreateOrUpdatePublicPermissions(string repo, string path) {
            string prefix = FindPrefixFromOrigin();

            // create permissions based on the repo and path
            string permissionTargetKey = string.Format("shr-{0}-{1}-{2}.pub", prefix, repo.Replace(".", "").Replace(" ", ""), path.GetHashCode());
            string permissionTargetlRepoDescription = string.Format("Public sharing permissions on {0} in {1} for repo {2}", repo, Origin, repo);
            string permissionsTargetIncludePattern = string.Format("{0}/**", path);

            PermissionTarget permissionTarget = storeBaseUrl.Security().GetPermissionTargetDetails(permissionTargetKey);

            // some persmissions already exist for that repo
            if (permissionTarget != null) {
                if (permissionTarget.Repositories.Count() > 1 || !permissionTarget.Repositories.Contains(repo)) {
                    log.ErrorFormat("Permission target already exists on artifactory but not for the right repo : {0} [{1}]. Please contact the administrator.", permissionTargetKey, repo);
                    throw new ShareException(string.Format("Permission target already exists on artifactory but not for the right repo : {0} [{1}]. Please contact the administrator.", permissionTargetKey, repo));
                }
                // if the permission does not exist
                if (!permissionTarget.IncludesPattern.Split(',').Any(ip => ip.Trim(' ') == permissionsTargetIncludePattern)) {
                    log.DebugFormat("Updating already existing permission target on artifactory : {0}", permissionTargetKey);
                    permissionTarget.IncludesPattern += "," + permissionsTargetIncludePattern;
                }
            } else {
                permissionTarget = new PermissionTarget() {
                    Name = permissionTargetKey,
                    Repositories = new System.Collections.Generic.List<string>() { repo },
                    IncludesPattern = permissionsTargetIncludePattern
                };
            }

            Permissions permissions = new Permissions();
            permissions.Add("r");
            permissionTarget.Principals.Groups.Clear();
            permissionTarget.Principals.Groups.Add("readers", permissions);
            permissionTarget.Principals.Users.Clear();
            permissionTarget.Principals.Users.Add("anonymous", permissions);

            storeBaseUrl.Security().CreateOrReplacePermissionTarget(permissionTarget);
        }



        private void CreateOrUpdateRestrictedPermissions(string repo, string path, bool addPermissions = false) {
            string prefix = FindPrefixFromOrigin();

            // create permissions based on the repo and path
            string permissionTargetKey = string.Format("shr-{0}-{1}-{2}.reader", prefix, repo.Replace(".", "").Replace(" ", ""), path.GetHashCode());
            string permissionTargetlRepoDescription = string.Format("Sharing permissions on {0} in {1} for repo {2}", repo, Origin, repo);
            string permissionsTargetIncludePattern = string.Format("{0}/**", path);

            PermissionTarget permissionTarget = storeBaseUrl.Security().GetPermissionTargetDetails(permissionTargetKey);

            // some persmissions already exist for that repo
            if (permissionTarget != null) {
                if (permissionTarget.Repositories.Count() > 1 || !permissionTarget.Repositories.Contains(repo)) {
                    log.ErrorFormat("Permission target already exists on artifactory but not for the right repo : {0} [{1}]. Please contact the administrator.", permissionTargetKey, repo);
                    throw new ShareException(string.Format("Permission target already exists on artifactory but not for the right repo : {0} [{1}]. Please contact the administrator.", permissionTargetKey, repo));
                }
                // if the permission does not exist
                if (!permissionTarget.IncludesPattern.Split(',').Any(ip => ip.Trim(' ') == permissionsTargetIncludePattern)) {
                    log.DebugFormat("Updating already existing permission target on artifactory : {0}", permissionTargetKey);
                    permissionTarget.IncludesPattern += "," + permissionsTargetIncludePattern;
                }
            } else {
                permissionTarget = new PermissionTarget() {
                    Name = permissionTargetKey,
                    Repositories = new System.Collections.Generic.List<string>() { repo },
                    IncludesPattern = permissionsTargetIncludePattern
                };
            }

            foreach (var community in Communities) {
                string communityGroupName = string.Format("{0}-co-{1}", prefix, community.Identifier);
                Permissions permissions = new Permissions();
                if (addPermissions && permissionTarget.Principals.Groups.ContainsKey(communityGroupName))
                    permissions = permissionTarget.Principals.Groups[communityGroupName];
                if (!permissions.Contains("r"))
                    permissions.Add("r");
                permissionTarget.Principals.Groups.Remove(communityGroupName);
                permissionTarget.Principals.Groups.Add(communityGroupName, permissions);
            }

            foreach (var user in Users) {
                Permissions permissions = new Permissions();
                if (addPermissions && permissionTarget.Principals.Groups.ContainsKey(user.Username))
                    permissions = permissionTarget.Principals.Users[user.Username];
                if (!permissions.Contains("r"))
                    permissions.Add("r");
                permissionTarget.Principals.Users.Remove(user.Username);
                permissionTarget.Principals.Users.Add(user.Username, permissions);
            }

            storeBaseUrl.Security().CreateOrReplacePermissionTarget(permissionTarget);
        }

        private void FindRepoAndPathFromIdentifier(out string repo, out string path) {
            if (Identifier.StartsWith(AppSettings["RecastBaseUrl"])) {
                Uri recastUrl = new Uri(Identifier);
                var match = Regex.Match(recastUrl.AbsolutePath, @"(?:\/t2api)?(?:\/(?:describe|search|dc\/status))?\/?(?'repo'\S[^\/]*)\/(?'path'\S*)");
                if (match.Success) {
                    repo = match.Groups["repo"].Value;
                    path = match.Groups["path"].Value;
                    return;
                }
            }

            repo = null;
            path = null;
        }


        private void CreateOrUpdateCommunityVirtualRepo(string repo, string path) {

            string prefix = FindPrefixFromOrigin();

            foreach (var community in Communities) {

                string virtualRepoKey = string.Format("{0}-co-{1}", prefix, community.Identifier);
                string virtualRepoDescription = string.Format("Community shares {0} in {1}", community.Identifier, Origin);

                RepositoryConfiguration repositoryConfiguration = storeBaseUrl.Repositories().RepositoryConfiguration(virtualRepoKey);

                if (repositoryConfiguration != null) {
                    if (!(repositoryConfiguration is VirtualRepositoryConfiguration)) {
                        log.ErrorFormat("Repo already exists on artifactory but not as virtual : {0}. Please contact the administrator.", virtualRepoKey);
                        continue;
                    } else {
                        VirtualRepositoryConfiguration vrepositoryConfiguration = repositoryConfiguration as VirtualRepositoryConfiguration;
                        if (vrepositoryConfiguration.packageType != RepositoryConfiguration.PackageType.generic ||
                            !vrepositoryConfiguration.Repositories.Contains(repo)) {
                            log.DebugFormat("Updating already existing virtual repo on artifactory : {0}", virtualRepoKey);
                            vrepositoryConfiguration.Repositories.Add(repo);
                            storeBaseUrl.Repositories().UpdateRepositoryConfiguration(repositoryConfiguration);
                        }
                    }
                } else {
                    repositoryConfiguration = new VirtualRepositoryConfiguration() {
                        Key = virtualRepoKey,
                        Description = virtualRepoDescription,
                        packageType = RepositoryConfiguration.PackageType.generic,
                        Repositories = new System.Collections.Generic.List<string>() { repo }
                    };
                    storeBaseUrl.Repositories().CreateRepository(repositoryConfiguration);
                }
            }
        }

        private void SynchronizeCommunitiesAndUsers() {
            if (Communities != null && Communities.Count > 0)
                SynchronizeCommunities();

            if (Users != null && Users.Count > 0)
                SynchronizeUsers();

        }

        private void SynchronizeUsers() {

        }

        private void SynchronizeCommunities() {
            foreach (var community in Communities) {

                string prefix = FindPrefixFromOrigin();

                string storeGroupName = string.Format("{0}-co-{1}", prefix, community.Identifier);
                string storeGroupDescription = string.Format("Community {0} in {1}", community.Identifier, Origin);

                SecurityGroup artifactoryGroup = null;
                try {
                    artifactoryGroup = storeBaseUrl.Security().GetGroupDetails(storeGroupName);
                }catch(Exception e) {
                    log.ErrorFormat("Error to get groups details : {0}", e.Message);
                }

                if (artifactoryGroup != null) {
                    log.DebugFormat("Community already exists as a group on artifactory : {0}", storeGroupName);
                    storeBaseUrl.Security().DeleteGroup(storeGroupName);
                }
                artifactoryGroup = new SecurityGroup() {
                    Name = storeGroupName,
                    Description = storeGroupDescription
                };
                storeBaseUrl.Security().CreateOrReplaceGroup(artifactoryGroup);

                foreach (var user in community.Users) {
                    SecurityUser artifactoryUser = storeBaseUrl.Security().GetUserDetails(user.Username);
                    if (artifactoryUser == null)
                        continue;
                    if (!artifactoryUser.Groups.Contains(storeGroupName)) {
                        artifactoryUser.Groups.Add(storeGroupName);
                        storeBaseUrl.Security().UpdateUser(artifactoryUser);
                    }

                }
            }
        }

        private string FindPrefixFromOrigin() {
            if (Origin.StartsWith("hydrology", StringComparison.InvariantCultureIgnoreCase))
                return "hep";
            if (Origin.StartsWith("geohazards", StringComparison.InvariantCultureIgnoreCase))
                return "gep";

            throw new InvalidDataException(string.Format("Origin {0} is not allowed for sharing", Origin));
        }

        private void ValidateRequest() {
            switch (Type) {
                case "results":
                    break;
                default:
                    throw new InvalidDataException(string.Format("Type {0} is not supported for sharing", Type));
            }

            if (string.IsNullOrEmpty(Identifier))
                throw new InvalidDataException(string.Format("No identifier specified for sharing"));

            switch (Visibility) {
                case "public":
                    break;
                case "private":
                    break;
                case "restricted":
                    if (Communities == null && Users == null)
                        throw new InvalidDataException(string.Format("No communities or users specified for restricted sharing"));
                    break;
                default:
                    throw new InvalidDataException(string.Format("Visibility {0} is not supported for sharing", Visibility));
            }

            // erase non existing users
            SearchResultItem[] userList = storeBaseUrl.Security().ListUsers();

            System.Collections.Generic.List<string> users = userList.Select(r => r.Name).ToList();

            if (Communities != null) {
                foreach (var community in Communities) {
                    community.Users = community.Users.Where(u => users.Contains(u.Username)).ToList();
                }
            }
            if (Users != null) {
                Users = Users.Where(u => users.Contains(u.Username)).ToList();
            }

        }

        public class Community {
            public string Identifier;

            public System.Collections.Generic.List<User> Users;
        }

        public class User {
            public string Username;
        }

    }

    public class ShareException : Exception {
        public ShareException() {}
        public ShareException(string message) : base(message) {}
    }
}