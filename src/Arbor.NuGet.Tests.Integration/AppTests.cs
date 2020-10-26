using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.NuGet.NuSpec.GlobalTool;
using Arbor.NuGet.NuSpec.GlobalTool.Application;
using NuGet.Packaging;
using Serilog;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;
using Zio;
using Zio.FileSystems;

namespace Arbor.NuGet.Tests.Integration
{
    public class AppTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public AppTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        private static CancellationTokenSource CreateCancellation() =>
            new CancellationTokenSource(TimeSpan.FromMinutes(value: 1));

        private Logger CreateLogger() =>
            new LoggerConfiguration().WriteTo.TestOutput(_testOutputHelper)
                .CreateLogger();

        [Fact]
        public async Task WhenCreatingNuSpecWithCreateAndMissingOutputThenExitCodeShouldNotBe0()
        {
            string[] args = {"nuspec", "create", "--source-directory", @"C:\temp"};

            using var app = new App(args, CreateLogger(), new MemoryFileSystem(),  CreateCancellation());
            var exitCode = await app.ExecuteAsync();

            Assert.NotEqual(expected: 0, exitCode);
        }

        [Fact]
        public async Task WhenCreatingNuSpecWithMissingCommandThenExitCodeShouldNotBe0()
        {
            using var app = new App(new[] { "nuspec" }, CreateLogger(), new MemoryFileSystem(), CreateCancellation());
            var exitCode = await app.ExecuteAsync();

            Assert.NotEqual(expected: 0, exitCode);
        }

        [Fact]
        public async Task WhenCreatingNuSpecWithMissingOptionsThenExitCodeShouldNotBe0()
        {
            string[] args = {"nuspec", "create"};

            using var app = new App(args, CreateLogger(), new MemoryFileSystem(), CreateCancellation());
            var exitCode = await app.ExecuteAsync();

            Assert.NotEqual(expected: 0, exitCode);
        }

        [Fact]
        public async Task WhenCreatingNuSpecWithValidArgsThenExitCodeShouldBe0()
        {
            int exitCode;

            using (var cts = CreateCancellation())
            {
                using IFileSystem fileSystem = new MemoryFileSystem();
                await using var sourceDirectory = TempDirectory.Create(fileSystem);

                await fileSystem.WriteAllTextAsync(
                    UPath.Combine(sourceDirectory.Directory.FullName, "test.txt"),
                    "Hello world",
                    Encoding.UTF8,
                    cts.Token);

                await using var targetDirectory = TempDirectory.Create(fileSystem);
                using var logger = CreateLogger();
                var outputFile = UPath.Combine(targetDirectory.Directory.FullName, "result.nuspec");

                string[] args =
                {
                                "nuspec",
                                "create",
                                "--source-directory",
                                $"{sourceDirectory.Directory.FullName}",
                                "--output-file",
                                $"{outputFile}",
                                "--package-id",
                                "Arbor.Sample",
                                "--package-version",
                                "1.2.3"
                            };

                using var app = new App(args, CreateLogger(), fileSystem, cts, leaveFileSystemOpen: true);
                exitCode = await app.ExecuteAsync();

                Assert.True(fileSystem.FileExists(outputFile), $"File.Exists(outputFile) {{'{outputFile}'}}");

                string content = await fileSystem.ReadAllTextAsync(outputFile, Encoding.UTF8, cts.Token);

                logger.Information("Nuspec: {NewLine}{Content}", Environment.NewLine, content);
            }

            Assert.Equal(expected: 0, exitCode);
        }

        [Fact]
        public async Task WhenCreatingNuSpecAndPackageThenExitCodeShouldBe0()
        {
            using var cts = CreateCancellation();

            using IFileSystem fileSystem = new PhysicalFileSystem();
            await using var sourceDirectory = TempDirectory.Create(fileSystem);

            await fileSystem.WriteAllTextAsync(
                UPath.Combine(sourceDirectory.Directory.FullName, "test.txt"),
                "Hello world",
                Encoding.UTF8,
                cts.Token);

            await using var targetDirectory = TempDirectory.Create(fileSystem);
            using var logger = CreateLogger();
            var outputFile = UPath.Combine(targetDirectory.Directory.FullName, "result.nuspec");

            await using var packageDirectory = TempDirectory.Create(fileSystem);

            string[] args =
            {
                "nuspec",
                "create",
                "--source-directory",
                $"{sourceDirectory.Directory.FullName}",
                "--output-file",
                $"{outputFile.FullName}",
                "--package-id",
                "Arbor.Sample",
                "--package-version",
                "1.2.3",
                "--package-directory",
                $"{packageDirectory.Directory.FullName}"
            };

            using var app = new App(args, CreateLogger(), fileSystem, cts, leaveFileSystemOpen: true);
            int exitCode = await app.ExecuteAsync();

            Assert.Equal(0, exitCode);

            Assert.True(fileSystem.FileExists(outputFile), $"File.Exists(outputFile) {{'{outputFile}'}}");

            string content = await fileSystem.ReadAllTextAsync(outputFile, Encoding.UTF8, cts.Token);

            logger.Information("Nuspec: {NewLine}{Content}", Environment.NewLine, content);

            Assert.True(packageDirectory.Directory.Exists);
            var packagePath = packageDirectory.Directory.Path / "Arbor.Sample.1.2.3.nupkg";

            Assert.True(fileSystem.FileExists(packagePath), $"File.Exists(packagePath) {{'{packagePath}'}}");
            await using var packageStream = fileSystem.OpenFile(packagePath, FileMode.Open, FileAccess.Read);

            using PackageArchiveReader reader = new PackageArchiveReader(packageStream);

            var files = reader.GetFiles()
                .Where(file => !file.StartsWith("[")
                               && ! file.StartsWith("_")
                               && ! file.StartsWith("package/")
                               )
                .ToArray();

            logger.Information("Files in package: @{Files}", files);

            Assert.Equal(4, files.Length);
            Assert.Contains("Content/test.txt", files);
            Assert.Contains("contentFiles.json.sha512", files);
            Assert.Contains("contentFiles.json", files);
            Assert.Contains("Arbor.Sample.nuspec", files);
        }

        [Fact]
        public async Task WhenCreatingNuSpecWithVersionFileThenVersionShouldBeCorrect()
        {
            int exitCode;

            using (var cts = CreateCancellation())
            {
                using IFileSystem fileSystem = new PhysicalFileSystem();
                await using var versionTempDirectory = TempDirectory.Create(fileSystem);

                string jsonVersionFileContent = @"{
    ""version"": ""1.0"",
    ""keys"": [
      {
        ""key"": ""major"",
        ""value"": 1
      },
      {
        ""key"": ""minor"",
        ""value"": 2
      },
      {
        ""key"": ""patch"",
        ""value"": 3
      }
    ]
  }";

                var versionJsonPath = UPath.Combine(versionTempDirectory.Directory.Path, Guid.NewGuid() + "_version.json");

                await fileSystem.WriteAllTextAsync(versionJsonPath, jsonVersionFileContent, cancellationToken: cts.Token);

                await using var sourceDirectory = TempDirectory.Create(fileSystem);

                await fileSystem.WriteAllTextAsync(
                    UPath.Combine(sourceDirectory.Directory.FullName, "test.txt"),
                    "Hello world",
                    Encoding.UTF8,
                    cts.Token);

                await using var targetDirectory = TempDirectory.Create(fileSystem);
                using var logger = CreateLogger();
                var outputFile = UPath.Combine(targetDirectory.Directory.FullName, "result.nuspec");

                string[] args =
                {
                                "nuspec",
                                "create",
                                "--source-directory",
                                $"{sourceDirectory.Directory.FullName}",
                                "--output-file",
                                $"{outputFile}",
                                "--package-id",
                                "Arbor.Sample",
                                "--version-file",
                                versionJsonPath.FullName
                            };

                using var app = new App(args, CreateLogger(), fileSystem, cts, leaveFileSystemOpen: true);
                exitCode = await app.ExecuteAsync();

                Assert.Equal(0, exitCode);

                Assert.True(fileSystem.FileExists(outputFile), $"File.Exists(outputFile) {{'{outputFile}'}}");

                string content = await fileSystem.ReadAllTextAsync(outputFile, Encoding.UTF8, cts.Token);

                logger.Information("Nuspec: {NewLine}{Content}", Environment.NewLine, content);
            }

            Assert.Equal(expected: 0, exitCode);
        }

        [Fact]
        public async Task WhenCreatingNuSpecWithMsBuildVersionFileThenVersionShouldBeCorrect()
        {
            int exitCode;

            using (var cts = CreateCancellation())
            {
                using IFileSystem fileSystem = new PhysicalFileSystem();
                await using var versionTempDirectory = TempDirectory.Create(fileSystem);

                string jsonVersionFileContent = @"<Project>
 <PropertyGroup>
   <Version>3.2.1</Version>
 </PropertyGroup>
</Project>";

                var versionJsonPath = UPath.Combine(versionTempDirectory.Directory.Path, Guid.NewGuid() + ".Directory.Build.props");

                await fileSystem.WriteAllTextAsync(versionJsonPath, jsonVersionFileContent, cancellationToken: cts.Token);

                await using var sourceDirectory = TempDirectory.Create(fileSystem);

                await fileSystem.WriteAllTextAsync(
                    UPath.Combine(sourceDirectory.Directory.FullName, "test.txt"),
                    "Hello world",
                    Encoding.UTF8,
                    cts.Token);

                await using var targetDirectory = TempDirectory.Create(fileSystem);
                using var logger = CreateLogger();
                var outputFile = UPath.Combine(targetDirectory.Directory.FullName, "result.nuspec");

                string[] args =
                {
                                "nuspec",
                                "create",
                                "--source-directory",
                                $"{sourceDirectory.Directory.FullName}",
                                "--output-file",
                                $"{outputFile}",
                                "--package-id",
                                "Arbor.Sample",
                                "--msbuild-version-file",
                                versionJsonPath.FullName
                            };

                using var app = new App(args, CreateLogger(), fileSystem, cts, leaveFileSystemOpen: true);
                exitCode = await app.ExecuteAsync();

                Assert.Equal(0, exitCode);

                Assert.True(fileSystem.FileExists(outputFile), $"File.Exists(outputFile) {{'{outputFile}'}}");

                string content = await fileSystem.ReadAllTextAsync(outputFile, Encoding.UTF8, cts.Token);

                logger.Information("Nuspec: {NewLine}{Content}", Environment.NewLine, content);
            }

            Assert.Equal(expected: 0, exitCode);
        }

        [Fact]
        public async Task WhenRunningWithWithEmptyArgsThenExitCodeShouldNotBe0()
        {
            using var app = new App(Array.Empty<string>(), CreateLogger(), new MemoryFileSystem(),  CreateCancellation());
            var exitCode = await app.ExecuteAsync();

            Assert.NotEqual(expected: 0, exitCode);
        }
    }
}