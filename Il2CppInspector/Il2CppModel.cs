/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public class Il2CppModel
    {
        public Il2CppInspector Package { get; }
        public List<Assembly> Assemblies { get; } = new List<Assembly>();

        // List of all types ordered by their TypeDefinitionIndex
        public TypeInfo[] TypesByIndex { get; }

        public Il2CppModel(Il2CppInspector package) {
            Package = package;
            TypesByIndex = new TypeInfo[package.TypeDefinitions.Length];

            // Create Assembly objects from Il2Cpp package
            for (var image = 0; image < package.Images.Length; image++)
                Assemblies.Add(new Assembly(this, image));
        }

        // Get or generate a type from its IL2CPP binary type usage reference
        // (field, return type, generic type parameter etc.)
        public TypeInfo GetType(Il2CppType pType, MemberTypes memberType = MemberTypes.All) {
            switch (pType.type) {
                case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
                case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
                    // Classes defined in the metadata
                    return TypesByIndex[(int) pType.datapoint];

                case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
                case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
                case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
                case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
                    // Everything that requires special handling
                    return new TypeInfo(this, pType, memberType);

                default:
                    return GetTypeFromEnum(pType.type);
            }
        }

        // Basic primitive types
        public TypeInfo GetTypeFromEnum(Il2CppTypeEnum t) {
            if ((int)t >= Il2CppConstants.FullNameTypeString.Count)
                return null;

            var fqn = Il2CppConstants.FullNameTypeString[(int) t];
            return TypesByIndex.First(x => x.FullName == fqn);
        }
    }
}