using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Schema;
using Terradue.Portal;

namespace Terradue.Tep
{

    /// <summary>
    /// Domain Links
    /// </summary>
    /// <description>
    /// Domain Links object represent a set of name / url links associated to a domain
    /// </description>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    /// \ingroup DomainLinks
    public class DomainLinks : DataPackage
    {

        public static readonly int KINDRESOURCESETLINKS = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.DomainLinks"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public DomainLinks (IfyContext context) : base (context){
            this.Kind = KINDRESOURCESETLINKS;
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="identifier">Identifier.</param>
        public static new DomainLinks FromIdentifier (IfyContext context, string identifier)
        {
            DomainLinks result = new DomainLinks (context);
            result.Identifier = identifier;
            try {
                result.Load ();
            } catch (Exception e) {
                throw e;
            }
            return result;
        }

        public new void Store () {
            this.Kind = KINDRESOURCESETLINKS;
            base.Store ();
        }
    }
}

