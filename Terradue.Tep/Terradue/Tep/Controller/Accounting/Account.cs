using System;


/*! 
\defgroup TepAccounting Accounting
@{

This component makes the integrated accounting of the TEP.


Accounting to user/group accounts
---------------------------------

Periodically, it collects the usage records from the \ref ApelServer and use them to balance
the user accounts with credits.
The following figure describes this process.

\startuml "Accounting to user/group accounts sequence diagram"

  control "Portal Agent" as PA
  participant "Portal" as P
  database "Portal database" as PDB
  database "APEL reporting" as AR
  
  autonumber

  loop every n sec

      PA -> P : Start accounting adjustment
      activate P
      P <-> PDB : load last sync time
      P <-> PDB : load users and groups accounts
      P -> AR : request integrated accounting for users and groups
      activate AR
      AR -> AR : aggregate accounts
      AR -> P : accounts
      deactivate AR
      loop for every user and groups in accounts
        P <-> PDB : load rates for items in accounts
        P -> P : control eventual deposit
        P -> P : debit user or group with cost
        P -> P : credit user or group provider with cost
        P <-> PDB : save account
      end
      deactivate P

  end

\enduml



Credit Authorization and Quota restrictions
-------------------------------------------

The quota restriction is implemented using the balance of the user or group account used to query the resource.

The following figure describes the example of the portal controlling the cost of a processing service and then
check that the balance on the user account requesting it is sufficient.

\startuml "Quota control using credits example on WPS sequence diagram"

  actor "User" as U
  participant "Portal" as P
  database "Portal database" as PDB
  participant "WPS Service" as WPS
  
  autonumber

  U -> P : Request processing
  activate P
  P <-> PDB : load user account
  P -> WPS : execute request (quotation mode)
  activate WPS
  WPS -> WPS : quote processing based on params
  WPS -> P : WPS result (quotation)
  deactivate WPS
  P -> P : check user balance
  alt enough credit
    P -> WPS : execute request (normal mode)
    P <-> PDB : update account with deposit charged
    P -> U : request confirmation (id)
  else not enough credit
    P -> U : request refused
  end
  deactivate P

\enduml


\xrefitem int "Interfaces" "Interfaces" connects \ref ApelReporting to retrieve the aggregated usage records.

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence stores the user accounts in the database

\ingroup Tep

@}
*/

namespace Terradue.Tep {

    /// <summary>
    /// Account
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

