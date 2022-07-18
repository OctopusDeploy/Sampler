using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.ILRepack;
using Nuke.Common.Utilities.Collections;
using Nuke.Common.Tools.OctoVersion;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.CompressionTasks;

[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    const string CiBranchNameEnvVariable = "OCTOVERSION_CurrentBranch";

    [Parameter(
        "Whether to auto-detect the branch name - this is okay for a local build, but should not be used under CI.")]
    readonly bool AutoDetectBranch = IsLocalBuild;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitRepository] readonly GitRepository GitRepository;
    [Solution] readonly Solution Solution;

    [Parameter("Test filter expression", Name = "where")] readonly string TestFilter = string.Empty;

    [OctoVersion(BranchParameter = nameof(BranchName),
        AutoDetectBranchParameter = nameof(AutoDetectBranch),
        Framework = "net6.0")]
    public OctoVersionInfo OctoVersionInfo;

    [Parameter(
        "Branch name for OctoVersion to use to calculate the version number. Can be set via the environment variable " +
        CiBranchNameEnvVariable + ".",
        Name = CiBranchNameEnvVariable)]
    string BranchName { get; set; }

    AbsolutePath SourceDirectory => RootDirectory / "source";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PublishDirectory => RootDirectory / "publish";

    Target Clean => _ => _
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetVersion(OctoVersionInfo.FullSemVer)
                .SetAssemblyVersion(OctoVersionInfo.MajorMinorPatch)
                .SetInformationalVersion(OctoVersionInfo.InformationalVersion)
                .EnableNoRestore());
        });

    Target Publish => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetOutput(PublishDirectory));
        });

    Target Merge => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            var inputFolder = PublishDirectory;
            //var inputFolder = OctopusClientFolder / "bin" / Configuration / "net462";
            var outputFolder = ArtifactsDirectory;
            EnsureExistingDirectory(outputFolder);

            var files = inputFolder.GlobFiles("*.dll")
                .Select(x => x.ToString());

            ILRepackTasks.ILRepack(_ => _
                .SetAssemblies(inputFolder / "Sampler.exe")
                .AddAssemblies(files)
                .SetTarget(ILRepackTarget.exe)
                .SetOutput(outputFolder / "Sampler.exe")
                .EnableInternalize());
        });

    Target Zip => _ => _
        .DependsOn(Merge)
        .Executes(() =>
            CompressZip(ArtifactsDirectory, ArtifactsDirectory / "Sampler.zip"));

    Target Default => _ => _
        .DependsOn(Zip);

    public static int Main() => Execute<Build>(x => x.Default);

}
