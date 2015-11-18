using System;

namespace Terradue.Tep.WebServer {
    public class CustomMessages {
        public CustomMessages() {
        }
    }

    public class CustomErrorMessages {
        public const string WRONG_IDENTIFIER            = "The selected identifier does not exist";
        public const string ADMINISTRATOR_ONLY_ACTION   = "Only Administrator can perform this action";
        public const string WRONG_ACCESSKEY             = "Wrong Access Key";
    }
}

