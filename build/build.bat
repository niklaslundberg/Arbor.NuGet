@ECHO OFF
dotnet --info
SET Arbor.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.Build.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.Build.NuGet.Package.Artifacts.Suffix=
SET Arbor.Build.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.Build.NuGetPackageVersion=
SET Arbor.Build.Vcs.Branch.Name.Version.OverrideEnabled=true
SET Arbor.Build.Vcs.Branch.Name=%GITHUB_REF%
SET Arbor.Build.VariableOverrideEnabled=true
SET Arbor.Build.Artifacts.CleanupBeforeBuildEnabled=true
SET Arbor.Build.NetAssembly.Configuration=
SET Arbor.Build.MSBuild.NuGetRestore.Enabled=true
SET Arbor.Build.Tools.External.Xunit.NetCoreApp.Enabled=false
SET Arbor.Build.BuildNumber.UnixEpochSecondsEnabled=true
SET Arbor.Build.Tools.External.VisualStudio.Version.PreRelease.Enabled=true
SET Arbor.Build.Tools.External.MSBuild.AllowPrerelease.Enabled=true
SET Arbor.Build.Log.Level=Debug
SET Arbor.Build.Tools.External.MSBuild.DotNet.Enabled=true
SET Fallback.Version.Build=0
SET Arbor.Build.NuGet.ReinstallArborPackageEnabled=true
SET Arbor.Build.NuGet.VersionUpdateEnabled=false
SET Arbor.Build.Artifacts.PdbArtifacts.Enabled=true
SET Arbor.Build.NuGet.Package.CreateNuGetWebPackages.Enabled=true
SET Arbor.Build.NuGet.PackageUpload.PackageExcludeStartsWithPatterns=dotnet-
CALL dotnet arbor-build

EXIT /B %ERRORLEVEL%