using System;
using Terradue.Portal;

namespace Terradue.Tep
{

    public class ThematicGroup : Domain
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Tep.ThematicGroup"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public ThematicGroup (IfyContext context) : base (context) { }


    }

    public class ThematicGroupFactory {

        private IfyContext Context;

        public ThematicGroupFactory (IfyContext context) 
        {
            Context = context;
        }

        public void CreateThematicGroup (string identifier, string name) {
            ThematicGroup tg = new ThematicGroup (Context);
            tg.Identifier = identifier;
            tg.Name = name;
            tg.Store ();
        }

        public void CreateVisitorRole () {
            //Create Role
            var starterRole = new Role (Context);
            starterRole.Identifier = "visitor";
            starterRole.Name = "visitor";
            starterRole.Store ();

            //Add Privileges
            //Data Package -- All
            starterRole.IncludePrivileges (Privilege.Get (EntityType.GetEntityType (typeof (DataPackage))));


        }

        public void CreateStarterRole () {

            //Create Role
            var starterRole = new Role (Context);
            starterRole.Identifier = "starter";
            starterRole.Name = "starter";
            starterRole.Store ();

            //Add Privileges
            //Data Package -- All
            starterRole.IncludePrivileges(Privilege.Get(EntityType.GetEntityType (typeof (DataPackage))));

        }

        public void CreateExplorerRole () { }

    }
}
