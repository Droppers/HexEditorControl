using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Components;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

interface INuGetPublish : IPack, IHazGitRepository
{
    [Parameter] string NuGetSource => TryGetValue(() => NuGetSource) ?? "https://api.nuget.org/v3/index.json";
    [Parameter] [Secret] string NuGetApiKey => TryGetValue(() => NuGetApiKey);

    Target Publish => _ => _
        .DependsOn(Pack)
        .After(Pack)
        .OnlyWhenDynamic(() => GitRepository.IsOnMainOrMasterBranch() || GitRepository.IsOnDevelopBranch())
        .Executes(() =>
        {
            if (NuGetApiKey is null)
            {
                return;
            }

            DotNetNuGetPush(_ => _
                    .Apply(PushSettingsBase)
                    .Apply(PushSettings)
                    .CombineWith(PushPackageFiles, (_, v) => _
                        .SetTargetPath(v))
                    .Apply(PackagePushSettings),
                PushDegreeOfParallelism,
                PushCompleteOnFailure);
        });

    sealed Configure<DotNetNuGetPushSettings> PushSettingsBase => _ => _
        .SetSource(NuGetSource)
        .SetApiKey(NuGetApiKey);

    Configure<DotNetNuGetPushSettings> PushSettings => _ => _;
    Configure<DotNetNuGetPushSettings> PackagePushSettings => _ => _;

    IEnumerable<AbsolutePath> PushPackageFiles => PackagesDirectory.GlobFiles("*.nupkg");

    bool PushCompleteOnFailure => true;
    int PushDegreeOfParallelism => 5;
}