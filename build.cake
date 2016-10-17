//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0007"
#tool "nuget:?package=ILRepack&version=2.0.11"
#addin "nuget:?package=Newtonsoft.Json&version=9.0.1"
#addin "nuget:?package=SharpCompress&version=0.12.4"

using Path = System.IO.Path;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IO = System.IO;
using SharpCompress;
using SharpCompress.Common;
using SharpCompress.Writer;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var publishDir = "./publish";
var artifactsDir = "./artifacts";
var projectToPublish = "./source/Sampler";
var isContinuousIntegrationBuild = !BuildSystem.IsLocalBuild;

var gitVersionInfo = GitVersion(new GitVersionSettings {
    OutputType = GitVersionOutput.Json
});

var nugetVersion = gitVersionInfo.NuGetVersion;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    Information("Building Sampler v{0}", nugetVersion);
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("__Default")
    .IsDependentOn("__Clean")
    .IsDependentOn("__Restore")
    .IsDependentOn("__UpdateAssemblyVersionInformation")
    .IsDependentOn("__Build")
    .IsDependentOn("__Publish")
    .IsDependentOn("__Zip");

Task("__Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
    CleanDirectory(publishDir);
    CleanDirectories("./source/**/bin");
    CleanDirectories("./source/**/obj");
});

Task("__Restore")
    .Does(() => DotNetCoreRestore());

Task("__UpdateAssemblyVersionInformation")
    .WithCriteria(isContinuousIntegrationBuild)
    .Does(() =>
{
     GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true,
        UpdateAssemblyInfoFilePath = "./source/**/AssemblyInfo.cs"
    });

    Information("AssemblyVersion -> {0}", gitVersionInfo.AssemblySemVer);
    Information("AssemblyFileVersion -> {0}", $"{gitVersionInfo.MajorMinorPatch}.0");
    Information("AssemblyInformationalVersion -> {0}", gitVersionInfo.InformationalVersion);
    if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(gitVersionInfo.NuGetVersion);
    if(BuildSystem.IsRunningOnAppVeyor)
        AppVeyor.UpdateBuildVersion(gitVersionInfo.NuGetVersion);
});

Task("__Build")
    .IsDependentOn("__UpdateProjectJsonVersion")
    .IsDependentOn("__UpdateAssemblyVersionInformation")
    .Does(() =>
{
    DotNetCoreBuild("**/project.json", new DotNetCoreBuildSettings
    {
        Configuration = configuration
    });
});

Task("__Test")
    .Does(() =>
{
    GetFiles("**/*Tests/project.json")
        .ToList()
        .ForEach(testProjectFile => 
        {
            DotNetCoreTest(testProjectFile.ToString(), new DotNetCoreTestSettings
            {
                Configuration = configuration,
                WorkingDirectory = Path.GetDirectoryName(testProjectFile.ToString())
            });
        });
});

Task("__UpdateProjectJsonVersion")
    .WithCriteria(isContinuousIntegrationBuild)
    .Does(() =>
{
    var projectToPublishProjectJson = Path.Combine("projectToPublish", "project.json");
    Information("Updating {0} version -> {1}", projectToPublishProjectJson, nugetVersion);
    ModifyJson(projectToPublishProjectJson, json => json["version"] = nugetVersion);
});

private void ModifyJson(string jsonFile, Action<JObject> modify)
{
    var json = JsonConvert.DeserializeObject<JObject>(IO.File.ReadAllText(jsonFile));
    modify(json);
    IO.File.WriteAllText(jsonFile, JsonConvert.SerializeObject(json, Formatting.Indented));
}


Task("__Publish")
    .Does(() =>
{
    DotNetCorePublish(projectToPublish, new DotNetCorePublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDir
    });
});

Task("__Zip")
    .IsDependentOn("__Publish")
    .Does(() => {
        Zip(publishDir, Path.Combine(artifactsDir, $"Octopus.Sampler.{nugetVersion}.zip"));
    });



//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("__Default");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
