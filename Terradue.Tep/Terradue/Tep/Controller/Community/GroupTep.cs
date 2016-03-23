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

User notifications
------------------

This component notifies users about status change for the following items:
- The completion of a job in a service
- The modification of dataset in a data package
- The arrival in new dataset in a dynamic data package

The notification is configurable in the user profile and can be enabled as the following:
- Email notification
- Web notification (a box is deplayed during the navigation on the site)

\xrefitem dep "Dependencies" "Dependencies" uses \ref Authorisation to manage the users in the groupswith their roles and their access accordingly.

\ingroup Tep

@}

\addtogroup Authorisation
@{

#### Authorisation scheme tailoring for TEP ####

As described previously, the portal authorisation mechanism allows a great flexibility for managing users and groups and their permissions with the items in the system.
In order to enable all the requirements specific to the TEP, the \ref TepCommunity uses the \ref Security components as the following:

- \ref Terradue.Portal.User is an \ref Terradue.Tep.UserTep registered via the \ref Authentication mechanism integrated in the portal (e.g. EO-SSO)
- \ref Terradue.Portal.Group is a \ref Terradue.Tep.GroupTep regrouping a set of \ref Terradue.Tep.UserTep put together for organisational purpose. For instance, all Terradue staff users are grouped in the Terradue Group.
- \ref Terradue.Portal.Domain is named "Thematic Group" and englobes all users, groups and objects having a thematic scope in common. For instance, 
there could be a "Volcanoes" thematic group that would have expert users in volcanoes monitoring, the data collections used for monitoring them (e.g. Sentinel-2 and 3),
the features related to this domain (e.g. latest most important eruptions) and the all the processing services relative to volcanoes.
- The inital roles are defined as \ref Terradue.Tep.RoleTep.

Objects identified and used in TEP are 
- \ref Terradue.Portal.Series called "Data \ref Collection"
- \ref Terradue.Tep.DataPackage
- \ref Terradue.Portal.Service also called processing services and mainly implemented as \ref Terradue.Portal.WpsProcessOffering 
- Job representing an instance of a processing service execution
- \ref Terradue.Cloud.CloudProvider providing with \ref Terradue.Cloud.CloudAppliance 
- \ref Terradue.Tep.ThematicApplication that combines at user level the previous objects.
- \ref Terradue.Portal.Activity that records all the operations executed from the portal

#### Additional privilege and permissions for TEP ####

Specific privileges and permissions are implemented for TEP in order to control a precise operation on the platform.
The specific operations are described in the folowing table.

Object Type                       | Operation Name        | Description
--------------------------------- | --------------------- | ----------------------------------------------------------------------------------------
\ref Terradue.Tep.Collection      | Can Search In         | Allows to search for dataset in the collection
\ref Terradue.Tep.Collection      | Can Download          | Allows to download the raw dataset in the collection directly on user desktop
\ref Terradue.Tep.Collection      | Can Process           | Allows to use the data inside processing service or for processing purpose
\ref Terradue.Cloud.CloudProvider | Can Request Sandbox   | Allows to request for a Developer Cloud Sandbox via the related \ref Cloud Provider
\ref Terradue.Cloud.CloudProvider | Can Provision Cluster | Allows to provision a Cluster via the related \ref Cloud Provider


@}

*/

namespace Terradue.Tep {

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

        public GroupTep() : base(null) {
        }

        /// <summary>
        /// Members of the thematic group
        /// </summary>
        /// <value>contains members</value>
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

