# Arbor.NuGet

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
