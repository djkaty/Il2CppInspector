/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public class Il2CppModel
    {
        public Il2CppInspector Package { get; }
        public List<Assembly> Assemblies { get; } = new List<Assembly>();

        // List of all types from TypeDefs ordered by their TypeDefinitionIndex
        public TypeInfo[] TypesByDefinitionIndex { get; }

        // List of all types from TypeRefs ordered by instanceIndex
        public TypeInfo[] TypesByReferenceIndex { get; }

        // List of all types from MethodSpecs (closed generic types that can be instantiated)
        public Dictionary<int, TypeInfo> TypesByMethodSpecClassIndex { get; } = new Dictionary<int, TypeInfo>();

        // List of all methods from MethodSpecs (closed generic methods that can be called; does not need to be in a generic class)
        public Dictionary<TypeInfo, List<MethodInfo>> GenericMethods { get; } = new Dictionary<TypeInfo, List<MethodInfo>>();

        // List of all type definitions by fully qualified name (TypeDefs only)
        public Dictionary<string, TypeInfo> TypesByFullName { get; } = new Dictionary<string, TypeInfo>();

        // List of type references that are initialized via pointers in the image
        public ConcurrentDictionary<ulong, TypeInfo> TypesByVirtualAddress { get; } = new ConcurrentDictionary<ulong, TypeInfo>();

        // Every type
        public IEnumerable<TypeInfo> Types => new IEnumerable<TypeInfo>[] {TypesByDefinitionIndex, TypesByReferenceIndex, TypesByVirtualAddress.Values}
            .SelectMany(t => t).Where(t => t != null).Distinct();

        // List of all methods ordered by their MethodDefinitionIndex
        public MethodBase[] MethodsByDefinitionIndex { get; }

        // List of all generated CustomAttributeData objects by their instanceIndex into AttributeTypeIndices
        public ConcurrentDictionary<int, CustomAttributeData> AttributesByIndices { get; } = new ConcurrentDictionary<int, CustomAttributeData>();

        // Get an assembly by its image name
        public Assembly GetAssembly(string name) => Assemblies.FirstOrDefault(a => a.ShortName == name);

        // Create type model
        public Il2CppModel(Il2CppInspector package) {
            Package = package;
            TypesByDefinitionIndex = new TypeInfo[package.TypeDefinitions.Length];
            TypesByReferenceIndex = new TypeInfo[package.TypeReferences.Count];
            MethodsByDefinitionIndex = new MethodBase[package.Methods.Length];

            // Recursively create hierarchy of assemblies and types from TypeDefs
            // No code that executes here can access any type through a TypeRef (ie. via TypesByReferenceIndex)
            for (var image = 0; image < package.Images.Length; image++)
                Assemblies.Add(new Assembly(this, image));

            // Create and reference types from TypeRefs
            // Note that you can't resolve any TypeRefs until all the TypeDefs have been processed
            for (int typeRefIndex = 0; typeRefIndex < package.TypeReferences.Count; typeRefIndex++) {
                var typeRef = Package.TypeReferences[typeRefIndex];
                var referencedType = resolveTypeReference(typeRef);

                TypesByReferenceIndex[typeRefIndex] = referencedType;
            }

            // Create types and methods from MethodSpec (which incorporates TypeSpec in IL2CPP)
            foreach (var spec in Package.MethodSpecs) {
                TypeInfo declaringType;

                // Concrete instance of a generic class
                // If the class index is not specified, we will later create a generic method in a non-generic class
                if (spec.classIndexIndex != -1) {
                    if (!TypesByMethodSpecClassIndex.ContainsKey(spec.classIndexIndex))
                        TypesByMethodSpecClassIndex.Add(spec.classIndexIndex, new TypeInfo(this, spec));

                    declaringType = TypesByMethodSpecClassIndex[spec.classIndexIndex];
                }
                else
                    declaringType = MethodsByDefinitionIndex[spec.methodDefinitionIndex].DeclaringType;

                // Concrete instance of a generic method
                if (spec.methodIndexIndex != -1) {

                    // First generic method declaration in this class?
                    if (!GenericMethods.ContainsKey(declaringType))
                        GenericMethods.Add(declaringType, new List<MethodInfo>());

                    // TODO: Add generic method resolver here

                    // Get list of pointers to type parameters (both unresolved and concrete)
                    var genericTypeArguments = ResolveGenericArguments(spec.methodIndexIndex);
                }
            }
        }

        // Get generic arguments from either a type or method instanceIndex from a MethodSpec
        public List<TypeInfo> ResolveGenericArguments(int instanceIndex) => ResolveGenericArguments(Package.GenericInstances[instanceIndex]);
        public List<TypeInfo> ResolveGenericArguments(Il2CppGenericInst inst) {

            // Get list of pointers to type parameters (both unresolved and concrete)
            var genericTypeArguments = Package.BinaryImage.ReadMappedWordArray(inst.type_argv, (int)inst.type_argc);

            return genericTypeArguments.Select(a => GetTypeFromVirtualAddress((ulong) a)).ToList();
        }

        private TypeInfo resolveTypeReference(Il2CppType typeRef) {
            TypeInfo underlyingType;

            switch (typeRef.type) {
                // Classes defined in the metadata (reference to a TypeDef)
                case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
                case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
                    underlyingType = TypesByDefinitionIndex[typeRef.datapoint]; // klassIndex
                    break;

                // Constructed types
                case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
                case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                case Il2CppTypeEnum.IL2CPP_TYPE_PTR:

                // Generic type and generic method parameters
                case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
                case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:

                    underlyingType = new TypeInfo(this, typeRef);
                    break;

                // Primitive types
                default:
                    underlyingType = getTypeDefinitionFromTypeEnum(typeRef.type);
                    break;
            }

            // Create a reference type if necessary
            return typeRef.byref ? underlyingType.MakeByRefType() : underlyingType;
        }

        // Basic primitive types are specified via a flag value
        private TypeInfo getTypeDefinitionFromTypeEnum(Il2CppTypeEnum t) {
            if ((int) t >= Il2CppConstants.FullNameTypeString.Count)
                return null;

            var fqn = Il2CppConstants.FullNameTypeString[(int) t];
            return TypesByFullName[fqn];
        }

        // Type from a virtual address pointer
        // These are always nested types from references within another TypeRef
        // TODO: Eliminate GetTypeFromVirtualAddress() - use base and offset from MetadataRegistration.ptypes (Package.TypeReferences) instead
        public TypeInfo GetTypeFromVirtualAddress(ulong ptr) {
            if (TypesByVirtualAddress.ContainsKey(ptr))
                return TypesByVirtualAddress[ptr];

            var type = Package.BinaryImage.ReadMappedObject<Il2CppType>(ptr);
            var referencedType = resolveTypeReference(type);

            TypesByVirtualAddress.TryAdd(ptr, referencedType);
            return referencedType;
        }

        // The attribute index is an index into AttributeTypeRanges, each of which is a start-end range index into AttributeTypeIndices, each of which is a TypeIndex
        public int GetCustomAttributeIndex(Assembly asm, uint token, int customAttributeIndex) {
            // Prior to v24.1, Type, Field, Parameter, Method, Event, Property, Assembly definitions had their own customAttributeIndex field
            if (Package.Version <= 24.0)
                return customAttributeIndex;

            // From v24.1 onwards, token was added to Il2CppCustomAttributeTypeRange and each Il2CppImageDefinition noted the CustomAttributeTypeRanges for the image
            if (!Package.AttributeIndicesByToken[asm.ImageDefinition.customAttributeStart].TryGetValue(token, out var index))
                return -1;
            return index;
        }

        // Get the name of a metadata typeRef
        public string GetMetadataUsageName(MetadataUsage usage) {
            switch (usage.Type) {
                case MetadataUsageType.TypeInfo:
                case MetadataUsageType.Type:
                var type = TypesByReferenceIndex[usage.SourceIndex];
                return type.Name;

                case MetadataUsageType.MethodDef:
                var method = MethodsByDefinitionIndex[usage.SourceIndex];
                return $"{method.DeclaringType.Name}.{method.Name}";

                case MetadataUsageType.FieldInfo: 
                var fieldRef = Package.FieldRefs[usage.SourceIndex];
                type = TypesByReferenceIndex[fieldRef.typeIndex];
                var field = type.DeclaredFields.First(f => f.Index == type.Definition.fieldStart + fieldRef.fieldIndex);
                return $"{type.Name}.{field.Name}";

                case MetadataUsageType.StringLiteral:
                return Package.StringLiterals[usage.SourceIndex];

                case MetadataUsageType.MethodRef:
                var methodSpec = Package.MethodSpecs[usage.SourceIndex];
                method = MethodsByDefinitionIndex[methodSpec.methodDefinitionIndex];
                type = method.DeclaringType;
                return $"{type.Name}.{method.Name}";
            }
            throw new NotImplementedException("Unknown metadata usage type: " + usage.Type);
        }
    }
}