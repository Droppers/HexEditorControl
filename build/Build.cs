using System;
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
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using static Nuke.Common.Tools.Xunit.XunitTasks;

[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    [Parameter, Secret] readonly string NuGetApiKey;
    
    [GitRepository, Required] readonly GitRepository GitRepository;
    [Solution(SuppressBuildProjectCheck = true), Required] readonly Solution Solution;
    [GitVersion, Required] readonly GitVersion GitVersion;

    public static int Main () => Execute<Build>(x => x.Pack);

    AbsolutePath ArtifactsDirectory => RootDirectory / "Artifacts";

    IEnumerable<Project> TestProjects =>
        Solution.Projects.Where(x => x.Name.Contains(".Test", StringComparison.OrdinalIgnoreCase));

    Target Clean => _ => _
        .Executes(() =>
        {
            EnsureCleanDirectory(ArtifactsDirectory);
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
                .SetConfiguration("Release")
                .EnableNoLogo());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach (var project in TestProjects)
            {
                DotNetTest(s => s.SetProjectFile(project));
            }
        });

    Target Pack => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetOutputDirectory(ArtifactsDirectory)
                .SetConfiguration("Release")
                .EnableNoLogo()
                .EnableContinuousIntegrationBuild()
                .SetVersion(GitVersion.NuGetVersionV2));
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .After(Pack)
        .OnlyWhenDynamic(() => GitRepository.IsOnMainOrMasterBranch() || GitRepository.IsOnDevelopBranch() || GitRepository.IsOnReleaseBranch())
        .Executes(() =>
        {
            NuGetApiKey.NotNull();

            var packages = GlobFiles(ArtifactsDirectory, "*.nupkg");

            Assert.NotEmpty(packages.ToList());

            DotNetNuGetPush(s => s
                .SetApiKey(NuGetApiKey)
                .EnableSkipDuplicate()
                .SetSource("https://api.nuget.org/v3/index.json")
                .EnableNoSymbols()
                .CombineWith(packages,
                    (v, path) => v.SetTargetPath(path)));
        });

    T From<T>()
        where T : INukeBuild
        => (T)(object)this;
}
