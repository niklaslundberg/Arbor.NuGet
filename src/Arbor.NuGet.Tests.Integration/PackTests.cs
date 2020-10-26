using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.NuGet.NuSpec.GlobalTool;
using Arbor.NuGet.NuSpec.GlobalTool.Application;
using Serilog;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;
using Zio;
using Zio.FileSystems;

namespace Arbor.NuGet.Tests.Integration
{
    public class PackTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public PackTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        private static CancellationTokenSource CreateCancellation() =>
            new CancellationTokenSource(TimeSpan.FromMinutes(value: 1));

        private Logger CreateLogger() =>
            new LoggerConfiguration().WriteTo.TestOutput(_testOutputHelper)
                .CreateLogger();


        [Fact]
        public async Task WhenCreatingNuSpecWithValidArgsThenExitCodeShouldBe0()
        {
            int exitCode;

            using (var cts = CreateCancellation())
            {
                using IFileSystem fileSystem = new PhysicalFileSystem();
                using var sourceDirectory = TempDirectory.Create(fileSystem);

                await fileSystem.WriteAllTextAsync(
                    UPath.Combine(sourceDirectory.Directory.FullName, "test.txt"),
                    "Hello world",
                    Encoding.UTF8,
                    cts.Token);

                using var targetDirectory = TempDirectory.Create(fileSystem);
                using var logger = CreateLogger();
                var outputFile = UPath.Combine(targetDirectory.Directory.Path, "result.nuspec");

                using var packageTargetDirectory = TempDirectory.Create(fileSystem);
                var packageFile = UPath.Combine(packageTargetDirectory.Directory.Path, "result.nupkg");

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

                using var app = new App(args, logger, fileSystem, cts, leaveFileSystemOpen: true);
                exitCode = await app.ExecuteAsync();

                Assert.Equal(expected: 0, exitCode);

                string[] packArgs =
                {
                    "pack", "nuspec", "--nuspec-file", $"{outputFile}", "--output-file", $"{packageFile}"
                };

                Assert.True(fileSystem.FileExists(outputFile));

                using var packApp = new App(packArgs, logger, fileSystem, cts, leaveFileSystemOpen: true);
                exitCode = await packApp.ExecuteAsync();

                Assert.Equal(expected: 0, exitCode);

                Assert.True(fileSystem.FileExists(packageFile));
            }

            Assert.Equal(expected: 0, exitCode);
        }
    }
}