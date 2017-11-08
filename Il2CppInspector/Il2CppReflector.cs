/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public class Il2CppReflector
    {
        public Il2CppInspector Package { get; }
        public List<Assembly> Assemblies { get; } = new List<Assembly>();

        public Il2CppReflector(Il2CppInspector package) {
            Package = package;

            // Create Assembly objects from Il2Cpp package
            for (var image = 0; image < package.Metadata.Images.Length; image++)
                Assemblies.Add(new Assembly(this, image));
        }

        // Get the assembly in which a type is defined
        public Assembly GetAssembly(TypeInfo type) => Assemblies.FirstOrDefault(x => x.DefinedTypes.Contains(type));

        // Get a type from its IL2CPP type index
        public TypeInfo GetTypeFromIndex(int typeIndex) => Assemblies.SelectMany(x => x.DefinedTypes).FirstOrDefault(x => x.Index == typeIndex);

        // Get or generate a type from its IL2CPP binary type usage reference
        // (field, return type, generic type parameter etc.)
        public TypeInfo GetType(Il2CppType pType, MemberTypes memberType = MemberTypes.All) {
            switch (pType.type) {
                case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
                case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
                    // Classes defined in the metadata
                    return GetTypeFromIndex((int) pType.datapoint);

                case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
                case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
                case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
                case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
                    // Everything that requires special handling
                    return new TypeInfo(this, pType, memberType);

                default:
                    // Basic primitive types
                    if ((int) pType.type >= DefineConstants.FullNameTypeString.Count)
                        return null;

                    return Assemblies.SelectMany(x => x.DefinedTypes).First(x => x.FullName == DefineConstants.FullNameTypeString[(int)pType.type]);
            }
        }
    }
}