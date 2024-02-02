#tool "nuget:?package=NuGet.CommandLine&version=5.5.1"
//#addin nuget:?package=Cake.MinVer&version=4.0.0

var version = Argument("buildversion", "1.0.2");
var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");
var solutionFolder = "./";
var haFolder = "./src/HA";
var outputFolder = "../artifacts";
var packFolder = "../../Releases/nuget";
var nugetPackage = $"{packFolder}/HA.{version}.nupkg";
var nugetEnvApiKey = "NUGET_API_KEY";

//var settings = new MinVerSettings();
//var minver = MinVer(settings);
//var version = minver.Version.Replace("-alpha.0.1", "");
//var ver = new MinVerAutoIncrement(MinVerAutoIncrement.Minor).value;

Task("Clean")
    .Does(() => {
        var settings = new DotNetCleanSettings
        {
            Framework = "net6.0",
            Configuration = configuration,
            OutputDirectory = "./artifacts/"
        };
        DotNetClean(haFolder, settings);
    });

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
        DotNetRestore(solutionFolder);
  });

Task("Build") 
    .IsDependentOn("Restore")
    .Does((context) => {
        context.Information($"Version: {version}");
        DotNetBuild(solutionFolder, new DotNetBuildSettings {
            NoRestore = true,
            Configuration = configuration,
            NoIncremental = true,
            MSBuildSettings = new DotNetMSBuildSettings()
                .WithProperty("Version", version)
                .WithProperty("AssemblyVersion", version)
                .WithProperty("FileVersion", version)
        });
    });

Task("Test") 
    .IsDependentOn("Build")
    .Does(() => {
        DotNetTest(solutionFolder, new DotNetTestSettings {
            NoRestore = true,
            NoBuild = true,
            Configuration = configuration
        });
    });

Task("Publish") 
    .IsDependentOn("Build")
    .Does(() => {
        DotNetPublish(haFolder, new DotNetPublishSettings {
            NoRestore = true,
            Configuration = configuration,
            MSBuildSettings = new DotNetMSBuildSettings()
                .WithProperty("Version", version)
                .WithProperty("AssemblyVersion", version)
                .WithProperty("FileVersion", version)
        });
    });

Task("Pack") 
    .IsDependentOn("Build")
    .Does(() => {
        DotNetPack(haFolder, new DotNetPackSettings {
            NoRestore = true,
            NoBuild = true,
            Configuration = configuration,
			OutputDirectory = packFolder,
            MSBuildSettings = new DotNetMSBuildSettings()
                .WithProperty("Version", version)
                .WithProperty("AssemblyVersion", version)
                .WithProperty("FileVersion", version)
        });
    });

Task("PushNuget")
      .IsDependentOn("Pack")
      .Does(() => {
        NuGetPush(nugetPackage, new NuGetPushSettings {
            ApiKey = nugetEnvApiKey,
            Source = "https://api.nuget.org/v3/index.json",
        }); 
    });
 
RunTarget(target);


 