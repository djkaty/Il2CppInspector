/*
    Copyright 2017-2019 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Il2CppInspector.Reflection {
    public class Assembly
    {
        // IL2CPP-specific data
        public TypeModel Model { get; }
        public Il2CppImageDefinition ImageDefinition { get; }
        public Il2CppAssemblyDefinition AssemblyDefinition { get; }
        public Il2CppCodeGenModule ModuleDefinition { get; }
        public int Index { get; }

        // Custom attributes for this assembly
        public IEnumerable<CustomAttributeData> CustomAttributes => CustomAttributeData.GetCustomAttributes(this);

        // Fully qualified name of the assembly
        public string FullName { get; }

        // Display name of the assembly
        public string ShortName { get; }

        // Entry point method for the assembly
        public MethodInfo EntryPoint => throw new NotImplementedException();

        // List of types defined in the assembly
        public List<TypeInfo> DefinedTypes { get; } = new List<TypeInfo>();

        // Get a type from its string name (including namespace)
        public TypeInfo GetType(string typeName) => DefinedTypes.FirstOrDefault(x => x.FullName == typeName);

        // Initialize from specified assembly index in package
        public Assembly(TypeModel model, int imageIndex) {
            Model = model;
            ImageDefinition = Model.Package.Images[imageIndex];
            AssemblyDefinition = Model.Package.Assemblies[ImageDefinition.assemblyIndex];

            if (AssemblyDefinition.imageIndex != imageIndex)
                throw new InvalidOperationException("Assembly/image index mismatch");

            Index = ImageDefinition.assemblyIndex;
            ShortName = Model.Package.Strings[ImageDefinition.nameIndex];

            // Get full assembly name
            var nameDef = AssemblyDefinition.aname;
            var name = Model.Package.Strings[nameDef.nameIndex];
            var culture = Model.Package.Strings[nameDef.cultureIndex];
            if (string.IsNullOrEmpty(culture))
                culture = "neutral";
            var pkt = BitConverter.ToString(nameDef.publicKeyToken).Replace("-", "");
            if (pkt == "0000000000000000")
                pkt = "null";
            var version = string.Format($"{nameDef.major}.{nameDef.minor}.{nameDef.build}.{nameDef.revision}");

            FullName = string.Format($"{name}, Version={version}, Culture={culture}, PublicKeyToken={pkt.ToLower()}");

            if (ImageDefinition.entryPointIndex != -1) {
                // TODO: Generate EntryPoint method from entryPointIndex
            }

            // Find corresponding module (we'll need this for method pointers)
            ModuleDefinition = Model.Package.Modules?[ShortName];

            // Generate types in DefinedTypes from typeStart to typeStart+typeCount-1
            for (var t = ImageDefinition.typeStart; t < ImageDefinition.typeStart + ImageDefinition.typeCount; t++) {
                var type = new TypeInfo(t, this);

                // Don't add empty module definitions
                if (type.Name != "<Module>")
                    DefinedTypes.Add(type);
            }
        }

        public override string ToString() => FullName;
    }
}