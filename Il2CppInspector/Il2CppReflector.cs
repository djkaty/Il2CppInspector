using System;
using System.Collections.Generic;
using System.Linq;

namespace Il2CppInspector.Reflection
{
    public class Il2CppReflector
    {
        public List<Assembly> Assemblies { get; } = new List<Assembly>();

        public Il2CppReflector(Il2CppInspector package) {
            // Create Assembly objects from Il2Cpp package
            for (var image = 0; image < package.Metadata.Images.Length; image++)
                Assemblies.Add(new Assembly(package, image));
        }

        // Get the assembly in which a type is defined
        public Assembly GetAssembly(Type type) => Assemblies.FirstOrDefault(x => x.DefinedTypes.Contains(type));

        // Get a type from its IL2CPP type index
        public Type GetTypeFromIndex(int typeIndex) => Assemblies.SelectMany(x => x.DefinedTypes).FirstOrDefault(x => x.Index == typeIndex);
    }
}
