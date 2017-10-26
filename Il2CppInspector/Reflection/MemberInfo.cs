using System.Collections.Generic;
using System.Reflection;

namespace Il2CppInspector.Reflection {
    public abstract class MemberInfo
    {
        // Assembly that this member is defined in
        public Assembly Assembly { get; set; }

        // Custom attributes for this member
        public IEnumerable<CustomAttributeData> CustomAttributes { get; set; } // TODO

        // Type that this type is declared in for nested types
        public Type DeclaringType { get; set; } // TODO

        // What sort of member this is, eg. method, field etc.
        public MemberTypes MemberType { get; set; } // TODO

        // Name of the member
        public string Name { get; set; }

        // TODO: GetCustomAttributes etc.
    }
}