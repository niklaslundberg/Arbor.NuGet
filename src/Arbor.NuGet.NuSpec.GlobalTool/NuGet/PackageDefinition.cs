using NuGet.Versioning;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet;

public class PackageDefinition(PackageId packageId, SemanticVersion semanticVersion)
{
    public PackageId PackageId { get; } = packageId;

    public SemanticVersion SemanticVersion { get; } = semanticVersion;
}