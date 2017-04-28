using System;
using System.Text;
using ServiceStack.ServiceHost;
using Terradue.Portal;
using Terradue.Tep.WebServer;

namespace Terradue.Tep.WebServer {
    
    [Route("/user/{identifier}/public", "GET", Summary = "GET the user public info", Notes = "User is found from identifier")]
    public class UserGetPublicProfileRequestTep : IReturn<WebUserProfileTep> {
        [ApiMember(Name = "identifier", Description = "User identifier", ParameterType = "query", DataType = "string", IsRequired = true)]
        public string identifier { get; set; }
    }

    [Route("/user/{id}/admin", "GET", Summary = "GET the user", Notes = "User is found from id")]
    public class UserGetProfileAdminRequestTep : IReturn<WebUserProfileTep> {
        [ApiMember(Name = "id", Description = "User id", ParameterType = "query", DataType = "int", IsRequired = true)]
        public int id { get; set; }
    }

    [Route("/user/admin", "PUT", Summary = "PUT the user as admin", Notes = "User is found from id")]
    public class UserUpdateAdminRequestTep : WebUserTep, IReturn<WebUserProfileTep> {
    }

    [Route("/user/public", "GET", Summary = "GET the users public info", Notes = "User is found from id")]
    public class UserGetPublicProfilesRequestTep : IReturn<WebUserProfileTep> {
    }

    public class WebUserProfileTep : WebUserTep{
        [ApiMember(Name = "Gravatar", Description = "User Gravatar url", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Gravatar { get; set; }

        [ApiMember(Name = "Github", Description = "User Github name", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string Github { get; set; }

        [ApiMember(Name = "CreatedJobs", Description = "User nb of Created Jobs", ParameterType = "query", DataType = "int", IsRequired = false)]
        public int CreatedJobs { get; set; }

        [ApiMember(Name = "CreatedDataPackages", Description = "User nb of Created Data packages", ParameterType = "query", DataType = "int", IsRequired = false)]
        public int CreatedDataPackages { get; set; }

        [ApiMember(Name = "DefaultDataPackageItems", Description = "User nb of Items in Default Data packages", ParameterType = "query", DataType = "int", IsRequired = false)]
        public int DefaultDataPackageItems { get; set; }

        [ApiMember(Name = "FirstLoginDate", Description = "User first login date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string FirstLoginDate { get; set; }

        [ApiMember(Name = "LastLoginDate", Description = "User last login date", ParameterType = "query", DataType = "string", IsRequired = false)]
        public string LastLoginDate { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebUserTep"/> class.
        /// </summary>
        public WebUserProfileTep() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Tep.WebServer.WebUserTep"/> class.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public WebUserProfileTep(IfyWebContext context, UserTep entity) : base(context, entity) {
            try{
                var github = Terradue.Github.GithubProfile.FromId(context, this.Id);
                this.Github = github.Name;
                this.Gravatar = entity.GetAvatar();
            }catch(Exception){}
            if(String.IsNullOrEmpty(this.Gravatar)) 
                this.Gravatar = string.Format("http://www.gravatar.com/avatar/{0}", HashEmailForGravatar(string.IsNullOrEmpty(this.Email) ? "" : this.Email));
            DateTime timef = entity.GetFirstLoginDate();
            this.FirstLoginDate = (timef == DateTime.MinValue ? null : timef.ToString("U"));
            DateTime timel = entity.GetLastLoginDate();
            this.LastLoginDate = (timel == DateTime.MinValue ? null : timel.ToString("U"));

            context.AccessLevel = EntityAccessLevel.Administrator;
            EntityList<WpsJob> jobs = new EntityList<WpsJob>(context);
            jobs.UserId = this.Id;
            jobs.ItemVisibility = EntityItemVisibility.OwnedOnly;
            jobs.Load();
            CreatedJobs = jobs.Count;

            EntityList<DataPackage> dp = new EntityList<DataPackage>(context);
            dp.UserId = this.Id;
            dp.ItemVisibility = EntityItemVisibility.OwnedOnly;
            dp.Load();
            CreatedDataPackages = dp.Count;

            var dpdefault = DataPackage.GetTemporaryForUser(context, entity);
            DefaultDataPackageItems = dpdefault.Items.Count;

        }

        /// <summary>
        /// Tos the entity.
        /// </summary>
        /// <returns>The entity.</returns>
        /// <param name="context">Context.</param>
        public UserTep ToEntity(IfyContext context, UserTep input) {
            UserTep user = (input == null ? new UserTep(context) : input);
            base.ToEntity(context, user);

            return user;
        }

        /// Hashes an email with MD5.  Suitable for use with Gravatar profile
        /// image urls
        private string HashEmailForGravatar(string email)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.  
            var md5Hasher = System.Security.Cryptography.MD5.Create();

            // Convert the input string to a byte array and compute the hash.  
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(email));

            // Create a new Stringbuilder to collect the bytes  
            // and create a string.  
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string.  
            for(int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();  // Return the hexadecimal string. 
        }
    }
}

