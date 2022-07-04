using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.ProjectModel;
using Nuke.Components;

[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild, INuGetPublish
{
    [Solution] readonly Solution Solution;

    Solution IHazSolution.Solution => Solution; 

    public static int Main () => Execute<Build>(x => ((INuGetPublish)x).Publish);

    public IEnumerable<Project> TestProjects => Solution.AllProjects.Where(p =>
        p.SolutionFolder.Name.Contains("test", StringComparison.OrdinalIgnoreCase));
    
    T From<T>()
        where T : INukeBuild
        => (T)(object)this;
}
