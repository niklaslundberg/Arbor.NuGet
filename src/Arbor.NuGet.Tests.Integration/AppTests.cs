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
                using var sourceDirectory = TempDirectory.Create(fileSystem);

                await fileSystem.WriteAllTextAsync(
                    UPath.Combine(sourceDirectory.Directory.FullName, "test.txt"),
                    "Hello world",
                    Encoding.UTF8,
                    cts.Token);

                using var targetDirectory = TempDirectory.Create(fileSystem);
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

                string? content = await fileSystem.ReadAllTextAsync(outputFile, Encoding.UTF8, cts.Token);

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