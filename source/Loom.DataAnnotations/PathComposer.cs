namespace Loom.DataAnnotations
{
    internal static class PathComposer
    {
        public static string Compose(string basePath, string subPath)
        {
            return string.IsNullOrEmpty(basePath) ? subPath : $"{basePath}.{subPath}";
        }
    }
}
