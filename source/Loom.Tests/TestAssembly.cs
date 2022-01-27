using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Loom.EventSourcing.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize]

namespace Loom
{
    [TestClass]
    public static class TestAssembly
    {
        [AssemblyInitialize]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Reviewed")]
        public static Task Initialize(TestContext context)
        {
            return StorageEmulator.Initialize();
        }
    }
}
