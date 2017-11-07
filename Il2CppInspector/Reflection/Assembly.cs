/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;
using System.Linq;

namespace Il2CppInspector.Reflection {
    public class Assembly
    {
        // IL2CPP-specific data
        public Il2CppReflector Model { get; }
        public Il2CppImageDefinition Definition { get; }
        public int Index { get; }

        // TODO: CustomAttributes

        // Name of the assembly
        public string FullName { get; }

        // Entry point method for the assembly
        //public MethodInfo EntryPoint { get; } // TODO

        // List of types defined in the assembly
        public List<TypeInfo> DefinedTypes { get; } = new List<TypeInfo>();

        // Get a type from its string name (including namespace)
        public TypeInfo GetType(string typeName) => DefinedTypes.FirstOrDefault(x => x.FullName == typeName);

        // Initialize from specified assembly index in package
        public Assembly(Il2CppReflector model, int imageIndex) {
            Model = model;
            Definition = Model.Package.Metadata.Images[imageIndex];
            Index = Definition.assemblyIndex;
            FullName = Model.Package.Strings[Definition.nameIndex];

            if (Definition.entryPointIndex != -1) {
                // TODO: Generate EntryPoint method from entryPointIndex
            }

            // Generate types in DefinedTypes from typeStart to typeStart+typeCount-1
            for (var t = Definition.typeStart; t < Definition.typeStart + Definition.typeCount; t++)
                DefinedTypes.Add(new TypeInfo(Model.Package, t, this));
        }

        public override string ToString() => FullName;
    }
}