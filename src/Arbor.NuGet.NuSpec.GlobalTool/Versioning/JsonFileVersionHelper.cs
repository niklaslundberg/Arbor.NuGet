using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Arbor.KVConfiguration.JsonConfiguration;
using Arbor.NuGet.NuSpec.GlobalTool.Extensions;
using NuGet.Versioning;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.Versioning
{
    internal static class JsonFileVersionHelper
    {
        public static SemanticVersion? GetVersionFromJsonFile(UPath versionFile, IFileSystem fileSystem, ILogger logger)
        {
            try
            {
                string? path = fileSystem.ConvertPathToInternal(versionFile);
                var jsonFileReader = new JsonFileReader(path);

                var configurationItems = jsonFileReader.ReadConfiguration();

                const string major = nameof(major);
                const string minor = nameof(minor);
                const string patch = nameof(patch);

                var items = new Dictionary<string, int> { [major] = 0, [minor] = 0, [patch] = 0 };

                foreach (string key in items.Keys.ToArray())
                {
                    string? value = configurationItems.SingleOrDefault(pair =>
                        string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;

                    if (!int.TryParse(value, out int intValue) ||
                        intValue < 0)
                    {
                        throw new FormatException("Could not parse {Key} as a positive integer");
                    }

                    items[key] = intValue;
                }

                return new SemanticVersion(items[major], items[minor], items[patch]);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.Error(ex, "Could not get version from file {VersionFile}", versionFile);
                return null;
            }
        }

        public static SemanticVersion? GetVersionFromMsBuildFile(UPath versionFile, IFileSystem fileSystem, ILogger logger)
        {
            try
            {
                using var stream = fileSystem.OpenFile(versionFile, FileMode.Open, FileAccess.Read);

                var document = XDocument.Load(stream);

                var project = document.Element(XName.Get("Project")) ?? throw new InvalidOperationException($"Could not find project in file {versionFile.FullName}");

                var propertyGroups = project.Elements(XName.Get("PropertyGroup"));

                string? versionValue = propertyGroups.Elements(XName.Get("Version")).FirstOrDefault()?.Value;

                if (string.IsNullOrWhiteSpace(versionValue))
                {
                    throw new InvalidOperationException($"Could not find a version property in file {versionFile.FullName}");
                }

                if (!SemanticVersion.TryParse(versionValue, out var version))
                {
                    throw new InvalidOperationException($"Could not parse '{versionValue}' as a valid semantic version");
                }

                return version;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.Error(ex, "Could not get version from file {VersionFile}", versionFile);
                return null;
            }
        }
    }
}