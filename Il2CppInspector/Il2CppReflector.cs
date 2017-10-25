using System;
using System.Collections.Generic;

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
        public Assembly GetAssembly(Type type) {
            throw new NotImplementedException();
        }
    }
}
