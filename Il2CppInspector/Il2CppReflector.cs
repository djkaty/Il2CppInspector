using System;
using System.Collections.Generic;

namespace Il2CppInspector.Reflection
{
    public class Il2CppReflector
    {
        public List<Assembly> Assemblies { get; } = new List<Assembly>();

        // Factory instantiation via Il2CppReflector.Parse only
        private Il2CppReflector() { }

        public static Il2CppReflector Parse(Il2CppInspector package) {
            var r = new Il2CppReflector();

            // TODO: Populate reflection classes

            return r;
        }

        // Get the assembly in which a type is defined
        public Assembly GetAssembly(Type type) {
            throw new NotImplementedException();
        }
    }
}
