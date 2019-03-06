using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.NuGet.NuSpec.GlobalTool.Application;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace Arbor.NuGet.Tests.Integration
{
    public class AppTests
    {
        public AppTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private readonly ITestOutputHelper _testOutputHelper;

        private static CancellationTokenSource CreateCancellation()
        {
            return new CancellationTokenSource(TimeSpan.FromMinutes(1));
        }

        private Logger CreateLogger()
        {
            return new LoggerConfiguration().WriteTo.TestOutput(_testOutputHelper, LogEventLevel.Verbose)
                .CreateLogger();
        }

        [Fact]
        public async Task WhenCreatingNuSpecWithCreateAndMissingOutputThenExitCodeShouldNotBe0()
        {
            string[] args = {"nuspec", "create", "--source-directory", @"C:\temp"};

            using (var app = new App(args, CreateLogger(), CreateCancellation()))
            {
                var exitCode = await app.ExecuteAsync();

                Assert.NotEqual(0, exitCode);
            }
        }

        [Fact]
        public async Task WhenCreatingNuSpecWithMissingCommandThenExitCodeShouldNotBe0()
        {
            using (var app = new App(new[] {"nuspec"}, CreateLogger(), CreateCancellation()))
            {
                var exitCode = await app.ExecuteAsync();

                Assert.NotEqual(0, exitCode);
            }
        }

        [Fact]
        public async Task WhenCreatingNuSpecWithMissingOptionsThenExitCodeShouldNotBe0()
        {
            string[] args = {"nuspec", "create"};

            using (var app = new App(args, CreateLogger(), CreateCancellation()))
            {
                var exitCode = await app.ExecuteAsync();

                Assert.NotEqual(0, exitCode);
            }
        }

        [Fact]
        public async Task WhenCreatingNuSpecWithValidArgsThenExitCodeShouldBe0()
        {
            int exitCode;

            using (var cts = CreateCancellation())
            {
                using (var sourceDirectory = TempDirectory.Create())
                {
                    await File.WriteAllTextAsync(
                        Path.Combine(sourceDirectory.Directory.FullName, "test.txt"),
                        "Hello world",
                        Encoding.UTF8,
                        cts.Token);

                    using (var targetDirectory = TempDirectory.Create())
                    {
                        using (var logger = CreateLogger())
                        {
                            var outputFile = Path.Combine(targetDirectory.Directory.FullName, "result.nuspec");
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

                            using (var app = new App(args, CreateLogger(), cts))
                            {
                                exitCode = await app.ExecuteAsync();

                                Assert.True(File.Exists(outputFile), $"File.Exists(outputFile) {{'{outputFile}'}}");

                                var content = await File.ReadAllTextAsync(outputFile, Encoding.UTF8, cts.Token);

                                logger.Information("Nuspec: {NewLine}{Content}", Environment.NewLine, content);
                            }
                        }
                    }
                }
            }

            Assert.Equal(0, exitCode);
        }

        [Fact]
        public async Task WhenRunningWithWithEmptyArgsThenExitCodeShouldNotBe0()
        {
            using (var app = new App(Array.Empty<string>(), CreateLogger(), CreateCancellation()))
            {
                var exitCode = await app.ExecuteAsync();

                Assert.NotEqual(0, exitCode);
            }
        }
    }
}