using System;


/*! 
\defgroup TepAccounting Accounting
@{

This component makes the integrated accounting of the TEP.


Apel Accounting to user/group accounts
--------------------------------------

Periodically, it collects the usage records from the \ref ApelServer and use them to balance
the user accounts with credits.


<em>Here shall be a sequence diagram to describe the periodical user account balance</em>



Credit Authorization and Quota restrictions
-------------------------------------------

The quota restriction is implemented using the balance of the user or group account used to query the resource.


<em>Here shall be a sequence diagram to describe the credit authorization mechanism and the quota restriction</em>


\xrefitem int "Interfaces" "Interfaces" connects \ref ApelReporting to retrieve the aggregated usage records.

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence stores the user accounts in the database

\ingroup Tep

@}
*/

namespace Terradue.Tep.Controller {

    /// <summary>
    /// Account.
    /// </summary>
    /// <description>
    /// This object represents an account owned by a user or a group. Each account has a balance value to represent the credit
    /// avaialable for the owner.
    /// </description>
    /// \ingroup TepAccounting
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    public class Account {
        public Account() {
        }

        /// <summary>
        /// User the account belongs to.
        /// </summary>
        /// <value>can be owned by a User</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public UserTep UserOwner {
            get;
            set;
        }

        /// <summary>
        /// Group the account belongs to.
        /// </summary>
        /// <value>can be owned by a Group</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public GroupTep GroupOwner {
            get;
            set;
        }

        /// <summary>
        /// Credit balance of the account
        /// </summary>
        /// <value>The balance.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public double Balance {
            get;
            set;
        }
    }
}

