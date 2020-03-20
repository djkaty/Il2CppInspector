using System.Reflection;
using System.IO;

namespace Il2CppInspector.Outputs.Data
{
    public class ResourceReader
    {
        public static string ReadFileAsString(string name) {
            string resourceName = typeof(ResourceReader).Namespace + "." + name;
            Assembly assembly = Assembly.GetCallingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) {
                throw new FileNotFoundException(name);
            }
            using StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            return result;
        }
    }
}
