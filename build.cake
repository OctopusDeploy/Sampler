//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0007"
#tool "nuget:?package=ILRepack&version=2.0.11"
#addin "Cake.FileHelpers"

using Path = System.IO.Path;
using IO = System.IO;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var artifactsDir = "./artifacts/";
var publishDir = "./publish";


GitVersion gitVersionInfo;
string nugetVersion;


///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    gitVersionInfo = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json
    });

    if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(gitVersionInfo.NuGetVersion);

    nugetVersion = gitVersionInfo.NuGetVersion;

    Information("Building Sampler v{0}", nugetVersion);
    Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
    CleanDirectory(publishDir);
    CleanDirectories("./source/**/bin");
    CleanDirectories("./source/**/obj");
    CleanDirectories("./source/**/TestResults");
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
        DotNetCoreRestore("source");
    });


Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreBuild("./source", new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
    });
});

Task("DotnetPublish")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCorePublish("source/Sampler", new DotNetCorePublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDir
    });
});

Task("Merge")
    .IsDependentOn("DotnetPublish")
    .Does(() => {
        CreateDirectory(artifactsDir);

        var inputFolder = publishDir;
        var outputFolder = artifactsDir;
        CreateDirectory(outputFolder);
        ILRepack(
            $"{outputFolder}/Sampler.exe",
            $"{inputFolder}/Sampler.exe",
            IO.Directory.EnumerateFiles(inputFolder, "*.dll").Select(f => (FilePath) f),
            new ILRepackSettings { 
                Internalize = true, 
                Libs = new List<FilePath>() { inputFolder }
            }
        );
    });

Task("Zip")
    .IsDependentOn("Merge")
    .Does(() => {
        Zip($"{artifactsDir}", $"{artifactsDir}/Sampler.zip");
    });

Task("Default")
    .IsDependentOn("Zip");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
