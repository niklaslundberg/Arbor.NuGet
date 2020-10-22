# Arbor.NuGet

[![CI](https://github.com/niklaslundberg/Arbor.NuGet/workflows/CI/badge.svg)](https://github.com/niklaslundberg/Arbor.NuGet/actions?query=workflow%3ACI) [![Nuget](https://img.shields.io/nuget/v/Arbor.NuGet.GlobalTool)](https://www.nuget.org/packages/Arbor.NuGet.GlobalTool/)

[![CI](https://github.com/niklaslundberg/Arbor.NuGet/workflows/CI/badge.svg?branch=develop)](https://github.com/niklaslundberg/Arbor.NuGet/actions?query=workflow%3ACI)
[![MyGet (with prereleases)](https://img.shields.io/myget/arbor/vpre/Arbor.NuGet.GlobalTool?label=nuget%20preview%20%28myget%29)](https://www.myget.org/F/arbor/api/v2/package/Arbor.NuGet.GlobalTool/0.1.2-build.1603390949)


## Example usages

		nuspec create --source-directory=C:\Repository\Output\ --output-file=C:\Target\target.nuspec --package-id=test --package-version=1.2.3

		nuspec create --source-directory=C:\Repository\Output\ --output-file=C:\Target\target.nuspec --package-id=test --version-file=C:\Repository\version.json
 
## Version JSON file example

		{
			"version": "1.0",
			"keys": [
				{
					"key": "major",
					"value": 1
				},
				{
					"key": "minor",
					"value": 2
				},
				{
					"key": "patch",
					"value": 3
				}
			]
		}
