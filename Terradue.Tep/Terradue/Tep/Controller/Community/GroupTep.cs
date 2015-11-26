using System;
using Terradue.Portal;
using System.Collections.Generic;


/*! 
\defgroup TepCommunity Community
@{

This component manages the TEP users and thematic groups along with the user roles.

Thematic Group simplifies management of the user community and help focus the user objective
for thematical business within those groups. It is an important concept in the Thematic Exploitation Platform
to gather users around a specific aspect or an organization ot the thematic.

For instance, an instituion may create its own thematic group with its selected members and experts. It may also
have business relationship with a data or ICT provider that are representated as a role within the group.

\xrefitem dep "Dependencies" "Dependencies" uses \ref Authorisation to manage the users in the groupswith their roles and their access accordingly.

\ingroup Tep

@}
*/

namespace Terradue.Tep.Controller {

    /// <summary>
    /// TEP Group
    /// </summary>
    /// <description>
    /// A Thematic exploitation Platform group is an entity comprising multiple people with a specific role, that has a collective goal 
    /// and is linked to a thematic.
    /// 
    /// Typically, An organizatio may form a group.
    /// 
    /// </description>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    /// \ingroup TepCommunity
    public class GroupTep : Group {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public GroupTep() {
        }

        /// <summary>
        /// Members of the thematic group
        /// </summary>
        /// <value>The members.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        List<UserTep> Members {
            get;
            set;
        }

        /// <summary>
        /// Value indicating whether this <see cref="Terradue.Tep.Controller.GroupTep"/> is public.
        /// </summary>
        /// <value><c>true</c> if public; otherwise, <c>false</c>.</value>
        ///  \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        bool Public {
            get;
            set;
        }
    }
}

