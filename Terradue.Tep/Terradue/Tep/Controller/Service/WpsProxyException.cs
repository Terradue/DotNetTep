using System;
using System.Net;
using System.Runtime.Serialization;

namespace Terradue.Tep
{
    [Serializable]
    class WpsProxyException : Exception
    {
        public WpsProxyException()
        {
        }

        public WpsProxyException(string message) : base(message)
        {
        }

        public WpsProxyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WpsProxyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}