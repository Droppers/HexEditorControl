using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter, Secret] readonly string NuGetApiKey;
    [Parameter] readonly string NuGetSource = "https://api.nuget.org/v3/index.json";

    [Parameter, Secret] readonly string NuGetDevApiKey;
    [Parameter] readonly string NuGetDevSource = "https://api.nuget.org/v3/index.json";

    [GitRepository, Required] readonly GitRepository GitRepository;
    [Solution(SuppressBuildProjectCheck = true, GenerateProjects = true), Required] readonly Solution Solution;
    [GitVersion(Framework = "net5.0"), Required] readonly GitVersion GitVersion;

    public static int Main() => Execute<Build>(x => x.Test);

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath TestResultDirectory => RootDirectory / "test-results";

    IEnumerable<Project> TestProjects => Solution.GetProjects("*.Tests");

    IEnumerable<Project> PackProjects => new[]
    {
        // Shared
        Solution.Sources.Shared.HexControl_Core,
        Solution.Sources.Shared.HexControl_SharedControl,
        Solution.Sources.Shared.HexControl_Renderer_Direct2D,
        Solution.Sources.Shared.HexControl_Renderer_Skia,

        // Controls
        Solution.Sources.Controls.HexControl_Avalonia,
        Solution.Sources.Controls.HexControl_WinForms,
        Solution.Sources.Controls.HexControl_Wpf,
        Solution.Sources.Controls.HexControl_WinUI
    };

    bool IsTaggedBuild => GitRepository.Tags.Count > 0;

    Target Clean => _ => _
        .Executes(() =>
        {
            EnsureCleanDirectory(ArtifactsDirectory);
            EnsureCleanDirectory(TestResultDirectory);
        });


    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution)
                .EnableNoCache()
                .SetConfigFile(RootDirectory / "nuget.config"));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoLogo());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetConfiguration(Configuration)
                .SetResultsDirectory(TestResultDirectory)
                .CombineWith(TestProjects,
                    (v, project) => v.SetProjectFile(project.Path)));
        });

    Target Pack => _ => _
        .DependsOn(Test)
        .Triggers(Push)
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetOutputDirectory(ArtifactsDirectory)
                .SetConfiguration(Configuration)
                .EnableNoLogo()
                .EnableContinuousIntegrationBuild()
                .SetVersion(GitVersion.NuGetVersionV2)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .CombineWith(PackProjects, (v, project) => v.SetProject(project.Path)));
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .OnlyWhenDynamic(() =>
            (IsTaggedBuild && (GitRepository.IsOnMainOrMasterBranch() || GitRepository.IsOnReleaseBranch()))
            || GitRepository.IsOnDevelopBranch())
        .Executes(() =>
        {
            NuGetApiKey.NotNull();
            NuGetDevApiKey.NotNull();

            var packages = GlobFiles(ArtifactsDirectory, "*.nupkg");

            Assert.NotEmpty(packages.ToList());

            DotNetNuGetPush(s => s
                .SetApiKey(NuGetDevApiKey)
                .EnableSkipDuplicate()
                .SetSource(NuGetDevSource)
                .EnableNoSymbols()
                .CombineWith(packages,
                    (v, path) => v.SetTargetPath(path)));

            if (!GitRepository.IsOnDevelopBranch() && NuGetDevSource != NuGetSource)
            {
                DotNetNuGetPush(s => s
                    .SetApiKey(NuGetApiKey)
                    .EnableSkipDuplicate()
                    .SetSource(NuGetSource)
                    .EnableNoSymbols()
                    .CombineWith(packages,
                        (v, path) => v.SetTargetPath(path)));
            }
        });
}
