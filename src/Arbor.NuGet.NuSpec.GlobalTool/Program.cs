using System.Threading.Tasks;

using Arbor.NuGet.NuSpec.GlobalTool.Application;

namespace Arbor.NuGet.NuSpec.GlobalTool
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await AppStarter.CreateAndStartAsync(args);
        }
    }
}