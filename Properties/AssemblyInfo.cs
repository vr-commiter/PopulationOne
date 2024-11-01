using System.Reflection;
using MelonLoader;

[assembly: AssemblyTitle(PopulationOne_TrueGear.BuildInfo.Description)]
[assembly: AssemblyDescription(PopulationOne_TrueGear.BuildInfo.Description)]
[assembly: AssemblyCompany(PopulationOne_TrueGear.BuildInfo.Company)]
[assembly: AssemblyProduct(PopulationOne_TrueGear.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + PopulationOne_TrueGear.BuildInfo.Author)]
[assembly: AssemblyTrademark(PopulationOne_TrueGear.BuildInfo.Company)]
[assembly: AssemblyVersion(PopulationOne_TrueGear.BuildInfo.Version)]
[assembly: AssemblyFileVersion(PopulationOne_TrueGear.BuildInfo.Version)]
[assembly: MelonInfo(typeof(PopulationOne_TrueGear.Main), PopulationOne_TrueGear.BuildInfo.Name, PopulationOne_TrueGear.BuildInfo.Version, PopulationOne_TrueGear.BuildInfo.Author, PopulationOne_TrueGear.BuildInfo.DownloadLink)]
[assembly: MelonColor()]

// Create and Setup a MelonGame Attribute to mark a Melon as Universal or Compatible with specific Games.
// If no MelonGame Attribute is found or any of the Values for any MelonGame Attribute on the Melon is null or empty it will be assumed the Melon is Universal.
// Values for MelonGame Attribute can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonGame(null, null)]