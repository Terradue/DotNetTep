/*!

\namespace Terradue.Tep
@{
    Terradue.Tep Software Package provides with all the functionalities specific to the TEP.

    \xrefitem sw_version "Versions" "Software Package Version" 1.3.39

    \xrefitem sw_link "Links" "Software Package List" [Terradue.Tep](https://git.terradue.com/sugar/Terradue.Tep)

    \xrefitem sw_license "License" "Software License" [AGPL](https://git.terradue.com/sugar/Terradue.Tep/LICENSE)

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.OpenSearch

    \xrefitem sw_req "Require" "Software Dependencies" \ref ServiceStack

    \xrefitem sw_req "Require" "Software Dependencies" \ref log4net

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.Portal

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.Authentication.Umsso

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.Cloud

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.Github

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.Metadata.EarthObservation

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.News

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.OpenNebula

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.OpenSearch.GeoJson

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.OpenSearch.RdfEO

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.OpenSearch.Tumblr

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.OpenSearch.Twitter

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.ServiceModel.Ogc.OwsContext

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.ServiceModel.Syndication

    \xrefitem sw_req "Require" "Software Dependencies" \ref Terradue.WebService.Model
    

    \ingroup Tep
@}

*/


/*!
\defgroup Tep Tep Modules
@{

    This is a super component that encloses all Thematic Exploitation Platform related functional components. 
    Their main functionnalities are targeted to enhance the basic \ref Core functionalities for the thematic usage of the plaform.


@}
*/

using System.Reflection;
using System.Runtime.CompilerServices;
using NuGet4Mono.Extensions;

[assembly: AssemblyTitle("Terradue.Tep")]
[assembly: AssemblyDescription("Terradue Tep .Net library")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Terradue")]
[assembly: AssemblyProduct("Terradue.Tep")]
[assembly: AssemblyCopyright("Terradue")]
[assembly: AssemblyAuthors("Enguerran Boissier")]
[assembly: AssemblyProjectUrl("https://git.terradue.com/sugar/Terradue.Tep")]
[assembly: AssemblyLicenseUrl("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("1.3.39")]
[assembly: AssemblyInformationalVersion("1.3.39")]
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]