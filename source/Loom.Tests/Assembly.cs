using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize]

namespace Loom
{
    using System.Threading.Tasks;
    using Loom.EventSourcing.Azure;

    [TestClass]
    public static class Assembly
    {
        [AssemblyInitialize]
        public static Task Initialize(TestContext context)
        {
            return StorageEmulator.Initialize();
        }
    }
}
