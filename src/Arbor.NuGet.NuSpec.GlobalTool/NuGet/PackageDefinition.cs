using NuGet.Versioning;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet
{
    public class PackageDefinition
    {
        public PackageDefinition(PackageId packageId, SemanticVersion semanticVersion)
        {
            PackageId = packageId;
            SemanticVersion = semanticVersion;
        }

        public PackageId PackageId { get; }

        public SemanticVersion SemanticVersion { get; }
    }
}