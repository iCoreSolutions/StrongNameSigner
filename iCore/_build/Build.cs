using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;

//[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
   /// Support plugins are available for:
   ///   - JetBrains ReSharper        https://nuke.build/resharper
   ///   - JetBrains Rider            https://nuke.build/rider
   ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
   ///   - Microsoft VSCode           https://nuke.build/vscode

   public static int Main() => Execute<Build>(x => x.Pack);

   [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
   readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

   [Parameter("The URL to the NuGet feed to publish the generated NuGet package to.")]
   string FeedUri = "http://tfs.ad.icore.se:8080/tfs/DefaultCollection/_packaging/iCorePackages/nuget/v3/index.json";


   [Solution] readonly Solution Solution;
   [GitRepository] readonly GitRepository GitRepository;

   AbsolutePath SourceDirectory => RootDirectory / "src";
   AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
   AbsolutePath NuspecFile => RootDirectory / "iCore/Brutal.Dev.StrongNameSigner.Lib.nuspec";
   AbsolutePath OriginalNuSpec => SourceDirectory / @"Brutal.Dev.StrongNameSigner.Setup\StrongNameSigner.nuspec";

   AbsolutePath GeneratedNuPkgPath;

   Target Clean => _ => _
       .Before(Restore)
       .Executes(() =>
       {
          MSBuild(s => s
               .SetTargetPath(Solution)
               .SetTargets("Clean"));

          SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
          EnsureCleanDirectory(ArtifactsDirectory);
       });

   Target Restore => _ => _
       .Executes(() =>
       {
          MSBuild(s => s
               .SetTargetPath(Solution)
               .SetTargets("Restore"));
       });

   Target Compile => _ => _
       .DependsOn(Restore)
       .Executes(() =>
       {
          MSBuild(s => s
               .SetTargetPath(Solution)
               .SetTargets("Rebuild")
               .SetConfiguration(Configuration)
               .SetMaxCpuCount(Environment.ProcessorCount)
               .SetNodeReuse(IsLocalBuild));
       });

   Target Pack => _ => _
      .DependsOn(Clean)
      .DependsOn(Compile)
      .Executes(() =>
      {
         var originalNuSpec = XDocument.Load(OriginalNuSpec);
         var ns = originalNuSpec.Root.Name.Namespace;

         var versionValue = originalNuSpec.Root.Element(ns + "metadata")?.Element(ns + "version")?.Value;

         if (versionValue == null)
         {
            Logger.Error($"Unable to find version from original nuspec at {OriginalNuSpec}.");
            throw new Exception($"Unable to find version from original nuspec at {OriginalNuSpec}.");
         }

         Logger.Info($"Using NuGet package version {versionValue}");


         NuGetPack(s => s
            .SetTargetPath(NuspecFile)
            .SetVersion(versionValue)
            .SetSuffix("iCore")
            .SetConfiguration(Configuration)
            .SetOutputDirectory(ArtifactsDirectory)            
         );
      });

   Target Push => _ => _
      .DependsOn(Pack)
      .Executes(() =>
      {
         var nupkg = GlobFiles(ArtifactsDirectory, "*.nupkg");
         if (nupkg.Count() != 1)
            throw new InvalidOperationException($"Expected a single NuGet package in {ArtifactsDirectory} but found {nupkg.Count()}");

         NuGetPush(s => s
            .SetApiKey("VSTS")
            .SetSource(FeedUri)
            .SetTargetPath(nupkg.First())
         );
      });
}
