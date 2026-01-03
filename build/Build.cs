using System.Collections.Generic;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

sealed class Build : NukeBuild
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    [Parameter("Admin token for docker relaunch")] readonly string DockerAdminToken = string.Empty;
    [Parameter("Data directory for docker sqlite bind mount")] readonly string DockerDataDir = string.Empty;
    [Parameter("Rebuild images before docker relaunch")] readonly bool DockerBuild;
    [Solution] 
    readonly Solution Solution = null!;
    readonly AbsolutePath SourceDir    = RootDirectory / "src";
    readonly AbsolutePath ArtifactsDir = RootDirectory / "artifacts";
    readonly AbsolutePath ConfigDir    = RootDirectory / ".config";

    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    [UsedImplicitly]
    Target Clean => x => x
        .Description("Clean bin/tmp/obj and artifacts")
        .Before(Restore)
        .Executes(CleanAll);

    Target Restore => x => x
        .Description("Restore tools and solution")
        .Executes(RestoreAll);

    Target Compile => x => x
        .Description("Build solution")
        .DependsOn(Restore)
        .Executes(CompileAll);

    Target DockerRelaunch => x => x
        .Description("Relaunch docker compose with optional AdminToken/DataDir/Build")
        .Executes(RelaunchDocker);

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    void CleanAll()
    {
        RootDirectory.GlobDirectories("bin", "tmp").ForEach(p => p.DeleteDirectory());
        SourceDir.GlobDirectories("**/bin", "**/tmp", "**/obj").ForEach(p => p.DeleteDirectory());
        ArtifactsDir.CreateOrCleanDirectory();
    }

    void RestoreAll()
    {
        DotNetToolRestore(s => s.SetConfigFile(ConfigDir / "dotnet-tools.json"));
        DotNetRestore();
    }

    void CompileAll() =>
        DotNetBuild(s => s
            .SetProjectFile(Solution)
            .EnableDeterministic()
            .AddProperty("Configuration", Configuration));

    void RelaunchDocker()
    {
        var script = RootDirectory / "eng" / "relaunch_docker_container.ps1";
        var args = new List<string> { "-File", script.ToString() };
        if (DockerBuild) args.Add("-Build");
        if (!string.IsNullOrWhiteSpace(DockerAdminToken)) args.Add($"-AdminToken \"{DockerAdminToken}\"");
        if (!string.IsNullOrWhiteSpace(DockerDataDir)) args.Add($"-DataDir \"{DockerDataDir}\"");

        var process = ProcessTasks.StartProcess("pwsh", string.Join(" ", args));
        process.AssertZeroExitCode();
    }

    public static int Main() => Execute<Build>(x => x.Compile);
}
