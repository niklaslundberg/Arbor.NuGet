namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet
{
    public class PackageId
    {
        public PackageId(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }
}