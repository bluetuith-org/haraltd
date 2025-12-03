#!/usr/bin/env dotnet run --file

#:package ProcessX@1.5.6

using System.Runtime.InteropServices;
using Cysharp.Diagnostics;
using Zx;
using static Zx.Env;

useShell = false;
verbose = true;

const string projectName = "Haraltd";
const string csProjFile = projectName + ".csproj";

var projectFilePath = "";
foreach (var fileInfo in Directory.GetFiles(Directory.GetCurrentDirectory(), $"{csProjFile}", SearchOption.AllDirectories))
{
    projectFilePath = fileInfo;
    break;
}

if (string.IsNullOrEmpty(projectFilePath))
{
    log($"The csproj file was not found", ConsoleColor.Red);
    Environment.Exit(1);
}

workingDirectory = Path.GetDirectoryName(projectFilePath);
Directory.SetCurrentDirectory(workingDirectory!);

var publishDirectory = $"{Path.GetDirectoryName(workingDirectory!)}/artifacts-to-publish";
Directory.CreateDirectory(publishDirectory);

Console.Write("Git version is: ");
var gitTag = await "git describe --exact-match --tags --abbrev=0";
var archiveName = $"{projectName}".ToLowerInvariant();

var (ridMatchPattern, appToArchivePattern, extraDirectoryPattern) = ("", "", "");
string[] rids = [];

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    var appExtension = "exe";

    rids = ["win-x64", "win-arm64"];
    ridMatchPattern = "win-*";
    appToArchivePattern = $"{projectName}.{appExtension}";
    extraDirectoryPattern = "publish/";
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    var appExtension = "app";

    rids = ["osx-x64", "osx-arm64"];
    ridMatchPattern = "osx-*";
    appToArchivePattern = $"{projectName}.{appExtension}/";
}
else
{
    log("OS is not supported by this build script", ConsoleColor.Red);
}

log($"Running build script ({RuntimeInformation.OSDescription})", ConsoleColor.Blue);

foreach (var rid in rids)
{
    log($"Building for RID = {rid}", ConsoleColor.DarkCyan);

    log($"[{rid}] Starting restore...", ConsoleColor.Yellow);
    await $"dotnet restore -r {rid}";
    log($"[{rid}] Restore done.", ConsoleColor.Green);

    log($"[{rid}] Starting clean...", ConsoleColor.Yellow);
    await $"dotnet clean -c Release -r {rid}";
    log($"[{rid}] Clean done", ConsoleColor.Green);

    log($"[{rid}] Starting build...", ConsoleColor.Yellow);
    await $"dotnet publish -c Release -r {rid}";
    log($"[{rid}] Build done.", ConsoleColor.Green);

    log($"Build done for RID = {rid}", ConsoleColor.DarkCyan);
}

log("Generating artifacts to publish", ConsoleColor.Cyan);
foreach (var buildOutputDir in Directory.GetDirectories(Path.Join(workingDirectory!, "bin", "Release"), ridMatchPattern, SearchOption.AllDirectories))
{
    var ridName = Path.GetFileName(buildOutputDir);
    var archiveFullName = $"{publishDirectory}/{archiveName}-{gitTag}-{ridName}.zip";

    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        var appPath = $"{buildOutputDir}/{appToArchivePattern}";

        log($"[{ridName}] Ad-hoc signing files...", ConsoleColor.Yellow);
        foreach (var filesToSign in Directory.GetFiles(appPath, "", SearchOption.AllDirectories))
        {
            await $"codesign --force -s - {filesToSign}";
        }

        await $"codesign --force -s - {appPath}";
        log($"[{ridName}] Signing files done", ConsoleColor.Yellow);
    }

    await $"tar -acf {archiveFullName} -C {buildOutputDir}/{extraDirectoryPattern} {appToArchivePattern}";
    log($"[{ridName}] Generated {Path.GetFileName(archiveFullName)}", ConsoleColor.Magenta);
}
log("Generating artifacts done.", ConsoleColor.Cyan);

log("Publishing releases to GitHub", ConsoleColor.DarkBlue);

try
{
    await $"gh release create {gitTag} --notes-from-tag --title {string.Format("\"{0} {1}\"", projectName, gitTag)}";
}
catch (ProcessErrorException processError)
{
    log($"NOTE: Github release creation returned: {processError.Message}", ConsoleColor.DarkYellow);
}

await $"gh release upload {gitTag} {publishDirectory}/*.zip";

log("Published releases to GitHub", ConsoleColor.DarkBlue);

log("Finished running build script.", ConsoleColor.Blue);

