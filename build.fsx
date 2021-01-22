#r "paket:
nuget Fake.Core.Target
nuget Fake.Core.ReleaseNotes
nuget Fake.DotNet.Cli
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.Tools.Git //"

#load "./.fake/build.fsx/intellisense.fsx"

open Fake
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Tools
open System
open System.IO

let productName = "FsPublicSuffix"
let sln = "FsPublicSuffix.sln"
let srcGlob =__SOURCE_DIRECTORY__  @@ "src/**/*.??proj"
let testsGlob = __SOURCE_DIRECTORY__  @@ "tests/**/*.??proj"
let distDir = __SOURCE_DIRECTORY__  @@ "dist"
let distGlob = distDir @@ "*.nupkg"

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let releaseNotesFormatted =
    release.Notes
    |> Seq.map (sprintf "* %s\n")
    |> String.concat ""

Target.create "Clean" (fun _ ->
    [ "bin"; distDir ]
    |> Shell.cleanDirs

    !! srcGlob
    ++ testsGlob
    |> Seq.collect(fun p ->
        ["bin";"obj"]
        |> Seq.map(fun sp ->
            Path.GetDirectoryName p @@ sp)
        )
    |> Shell.cleanDirs
)

Target.create "DotnetRestore" (fun _ ->
    DotNet.restore (fun c ->
        { c with
            Common = c.Common
                     |> DotNet.Options.withAdditionalArgs [ sprintf "/p:PackageVersion=%s" release.NugetVersion ]
        }) sln
)

Target.create "DotnetBuild" (fun _ ->
    DotNet.build (fun c ->
        { c with
            Configuration = DotNet.BuildConfiguration.Release
            //This makes sure that Proj2 references the correct version of Proj1
            Common = c.Common
                     |> DotNet.Options.withAdditionalArgs [
                        sprintf "/p:PackageVersion=%s" release.NugetVersion
                        "--no-restore"
                     ]
        }) sln
)

Target.create "DotnetTest" (fun _ ->
    !! testsGlob
    |> Seq.iter (fun proj ->
        DotNet.test (fun c ->
            { c with
                Configuration = DotNet.BuildConfiguration.Release
                Common = c.Common
                         |> DotNet.Options.withAdditionalArgs [ "--no-build" ]
            }) proj
    )
)

Target.create "WatchTests" (fun _ ->
    !! testsGlob
    |> Seq.map(fun proj -> async {
        DotNet.exec
            (DotNet.Options.withWorkingDirectory (Path.GetDirectoryName proj))
            "watch run"
            ""
        |> ignore
    })
    |> Seq.iter (Async.Catch >> Async.Ignore >> Async.Start)

    printfn "Press Ctrl+C (or Ctrl+Break) to stop..."
    let cancelEvent = Console.CancelKeyPress |> Async.AwaitEvent |> Async.RunSynchronously
    cancelEvent.Cancel <- true
)

Target.create "AssemblyInfo" (fun _ ->
    let releaseChannel =
        match release.SemVer.PreRelease with
        | Some pr -> pr.Name
        | _ -> "release"
    let releaseDate =
        release.Date.Value

    let getAssemblyInfoAttributes projectName =
        [
          AssemblyInfo.Title (projectName)
          AssemblyInfo.Product productName
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.Metadata("ReleaseDate", releaseDate.ToString("o"))
          AssemblyInfo.FileVersion release.AssemblyVersion
          AssemblyInfo.InformationalVersion release.AssemblyVersion
          AssemblyInfo.Metadata("ReleaseChannel", releaseChannel)
          AssemblyInfo.Metadata("GitHash", Git.Information.getCurrentSHA1(null))
        ]

    let getProjectDetails projectPath =
        let projectName = Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    !! srcGlob
    ++ testsGlob
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, _, folderName, attributes) ->
        if projFileName.EndsWith("fsproj") then
            AssemblyInfoFile.createFSharp (folderName @@ "AssemblyInfo.fs") attributes
    )
)

Target.create "DotnetPack" (fun _ ->
    !! srcGlob
    |> Seq.iter (fun proj ->
        DotNet.pack (fun c ->
            { c with
                Configuration = DotNet.BuildConfiguration.Release
                OutputPath = Some distDir
                Common =
                    c.Common
                    |> DotNet.Options.withAdditionalArgs [
                        sprintf "/p:PackageVersion=%s" release.NugetVersion
                        sprintf "/p:PackageReleaseNotes=\"%s\"" releaseNotesFormatted
                    ]
            }) proj
    )
)

Target.create "SourcelinkTest" (fun _ ->
    !! distGlob
    |> Seq.iter (fun nupkg ->
        let result = DotNet.exec id "sourcelink" (sprintf "test \"%s\"" nupkg)
        if not result.OK then failwithf "sourcelink test failed: %A" result.Messages
    )
)

Target.create "GitRelease" (fun _ ->
    let releaseBranch = "master"
    if Git.Information.getBranchName "" <> releaseBranch then
        failwithf "Not on %s. If you want to release please switch to this branch." releaseBranch

    Git.Staging.stageAll ""
    Git.Commit.exec "" (sprintf "Bump version to %s \n%s" release.NugetVersion releaseNotesFormatted)
    Git.Branches.push ""

    Git.Branches.tag "" release.NugetVersion
    Git.Branches.pushTag "" "origin" release.NugetVersion
)

Target.create "Release" ignore

open TargetOperators

// Only call Clean if DotnetPack was in the call chain
// Ensure Clean is called before DotnetRestore
"Clean" ?=> "DotnetRestore"
"Clean" ==> "DotnetPack"

// Only call AssemblyInfo if Publish was in the call chain
// Ensure AssemblyInfo is called after DotnetRestore and before DotnetBuild
"DotnetRestore" ?=> "AssemblyInfo"
"AssemblyInfo" ?=> "DotnetBuild"

"DotnetRestore"
  ==> "DotnetBuild"
  ==> "DotnetTest"
  ==> "DotnetPack"
  ==> "SourcelinkTest"
  ==> "GitRelease"
  ==> "Release"

"DotnetRestore"
 ==> "WatchTests"

Target.runOrDefaultWithArguments "DotnetPack"
