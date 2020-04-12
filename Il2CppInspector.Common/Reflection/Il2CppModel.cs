/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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

        // List of all methods from MethodSpecs (closed generic methods that can be called; does not need to be in a generic class)
        public Dictionary<Il2CppMethodSpec, MethodBase> GenericMethods { get; } = new Dictionary<Il2CppMethodSpec, MethodBase>();

        // List of all type definitions by fully qualified name (TypeDefs only)
        public Dictionary<string, TypeInfo> TypesByFullName { get; } = new Dictionary<string, TypeInfo>();

        // Every type
        public IEnumerable<TypeInfo> Types {
            get {
                var result = new IEnumerable<TypeInfo>[] { TypesByDefinitionIndex, TypesByReferenceIndex }.SelectMany(t => t);
                result = result.Concat(result.SelectMany(t => t.CachedGeneratedTypes));
                return result.Distinct();
            }
        }

        // List of all methods ordered by their MethodDefinitionIndex
        public MethodBase[] MethodsByDefinitionIndex { get; }

        // List of all Method.Invoke functions by invoker index
        public MethodInvoker[] MethodInvokers { get; }

        // List of all generated CustomAttributeData objects by their instanceIndex into AttributeTypeIndices
        public ConcurrentDictionary<int, CustomAttributeData> AttributesByIndices { get; } = new ConcurrentDictionary<int, CustomAttributeData>();

        // Get an assembly by its image name
        public Assembly GetAssembly(string name) => Assemblies.FirstOrDefault(a => a.ShortName == name);

        // Get a type by its fully qualified name including generic type arguments, array brackets etc.
        // In other words, rather than only being able to fetch a type definition such as in Assembly.GetType(),
        // this method can also find reference types, types created from TypeRefs and constructed types from MethodSpecs
        public TypeInfo GetType(string fullName) => Types.FirstOrDefault(t => fullName == t.Namespace + "." + t.Name);

        // Get a concrete instantiation of a generic method from its fully qualified name and type arguments
        public MethodBase GetGenericMethod(string fullName, params TypeInfo[] typeArguments) =>
            GenericMethods.Values.First(m => fullName == m.DeclaringType.Namespace + "." + m.DeclaringType.BaseName + "." + m.Name
            && m.GetGenericArguments().SequenceEqual(typeArguments));

        // Create type model
        public Il2CppModel(Il2CppInspector package) {
            Package = package;
            TypesByDefinitionIndex = new TypeInfo[package.TypeDefinitions.Length];
            TypesByReferenceIndex = new TypeInfo[package.TypeReferences.Count];
            MethodsByDefinitionIndex = new MethodBase[package.Methods.Length];
            MethodInvokers = new MethodInvoker[package.MethodInvokePointers.Length];

            // Recursively create hierarchy of assemblies and types from TypeDefs
            // No code that executes here can access any type through a TypeRef (ie. via TypesByReferenceIndex)
            for (var image = 0; image < package.Images.Length; image++)
                Assemblies.Add(new Assembly(this, image));

            // Create and reference types from TypeRefs
            // Note that you can't resolve any TypeRefs until all the TypeDefs have been processed
            for (int typeRefIndex = 0; typeRefIndex < package.TypeReferences.Count; typeRefIndex++) {
                if(TypesByReferenceIndex[typeRefIndex] != null) {
                    /* type already generated - probably by forward reference through GetTypeFromVirtualAddress */
                    continue;
                }

                var typeRef = Package.TypeReferences[typeRefIndex];
                var referencedType = TypeInfo.FromTypeReference(this, typeRef);

                TypesByReferenceIndex[typeRefIndex] = referencedType;
            }

            // Create types and methods from MethodSpec (which incorporates TypeSpec in IL2CPP)
            foreach (var spec in Package.MethodSpecs) {
                TypeInfo declaringType;

                // Concrete instance of a generic class
                // If the class index is not specified, we will later create a generic method in a non-generic class
                if (spec.classIndexIndex != -1) {
                    var genericTypeDefinition = MethodsByDefinitionIndex[spec.methodDefinitionIndex].DeclaringType;
                    var genericInstance = Package.GenericInstances[spec.classIndexIndex];
                    var genericArguments = ResolveGenericArguments(genericInstance);
                    declaringType = genericTypeDefinition.MakeGenericType(genericArguments);
                }
                else
                    declaringType = MethodsByDefinitionIndex[spec.methodDefinitionIndex].DeclaringType;

                // Concrete instance of a generic method
                if (spec.methodIndexIndex != -1) {
                    // Method or constructor
                    var concreteMethod = new MethodInfo(this, spec, declaringType);
                    if (concreteMethod.Name == ConstructorInfo.ConstructorName || concreteMethod.Name == ConstructorInfo.TypeConstructorName)
                        GenericMethods.Add(spec, new ConstructorInfo(this, spec, declaringType));
                    else
                        GenericMethods.Add(spec, concreteMethod);
                }
            }

            // Find all custom attribute generators (populate AttributesByIndices) (use ToList() to force evaluation)
            var allAssemblyAttributes = Assemblies.Select(a => a.CustomAttributes).ToList();
            var allTypeAttributes = TypesByDefinitionIndex.Select(t => t.CustomAttributes).ToList();
            var allEventAttributes = TypesByDefinitionIndex.SelectMany(t => t.DeclaredEvents).Select(e => e.CustomAttributes).ToList();
            var allFieldAttributes = TypesByDefinitionIndex.SelectMany(t => t.DeclaredFields).Select(f => f.CustomAttributes).ToList();
            var allPropertyAttributes = TypesByDefinitionIndex.SelectMany(t => t.DeclaredProperties).Select(p => p.CustomAttributes).ToList();
            var allMethodAttributes = MethodsByDefinitionIndex.Select(m => m.CustomAttributes).ToList();
            var allParameterAttributes = MethodsByDefinitionIndex.SelectMany(m => m.DeclaredParameters).Select(p => p.CustomAttributes).ToList();

            // Create method invokers (one per signature, in invoker index order)
            foreach (var method in MethodsByDefinitionIndex) {
                var index = package.GetInvokerIndex(method.DeclaringType.Assembly.ModuleDefinition, method.Definition);
                if (index != -1) {
                    if (MethodInvokers[index] == null)
                        MethodInvokers[index] = new MethodInvoker(method, index);

                    method.Invoker = MethodInvokers[index];
                }
            }

            // TODO: Some invokers are not initialized or missing, need to find out why
            // Create method invokers sourced from generic method invoker indices
            foreach (var spec in GenericMethods.Keys) {
                if (package.GenericMethodInvokerIndices.TryGetValue(spec, out var index)) {
                    if (MethodInvokers[index] == null)
                        MethodInvokers[index] = new MethodInvoker(GenericMethods[spec], index);

                    GenericMethods[spec].Invoker = MethodInvokers[index];
                }
            }
        }

        // Get generic arguments from either a type or method instanceIndex from a MethodSpec
        public TypeInfo[] ResolveGenericArguments(Il2CppGenericInst inst) {

            // Get list of pointers to type parameters (both unresolved and concrete)
            var genericTypeArguments = Package.BinaryImage.ReadMappedWordArray(inst.type_argv, (int)inst.type_argc);
            
            return genericTypeArguments.Select(a => GetTypeFromVirtualAddress((ulong) a)).ToArray();
        }

        // Get a TypeRef by its virtual address
        // These are always nested types from references within another TypeRef
        public TypeInfo GetTypeFromVirtualAddress(ulong ptr) {
            var typeRefIndex = Package.TypeReferenceIndicesByAddress[ptr];

            if (TypesByReferenceIndex[typeRefIndex] != null)
                return TypesByReferenceIndex[typeRefIndex];

            var type = Package.TypeReferences[typeRefIndex];
            var referencedType = TypeInfo.FromTypeReference(this, type);

            TypesByReferenceIndex[typeRefIndex] = referencedType;
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
                    return GetMetadataUsageType(usage).Name;

                case MetadataUsageType.MethodDef:
                    var method = GetMetadataUsageMethod(usage);
                    return $"{method.DeclaringType.Name}.{method.Name}";

                case MetadataUsageType.FieldInfo: 
                    var fieldRef = Package.FieldRefs[usage.SourceIndex];
                    var type = GetMetadataUsageType(usage);
                    var field = type.DeclaredFields.First(f => f.Index == type.Definition.fieldStart + fieldRef.fieldIndex);
                    return $"{type.Name}.{field.Name}";

                case MetadataUsageType.StringLiteral:
                    return Package.StringLiterals[usage.SourceIndex];

                case MetadataUsageType.MethodRef:
                    type = GetMetadataUsageType(usage);
                    method = GetMetadataUsageMethod(usage);
                    return $"{type.Name}.{method.Name}";
            }
            throw new NotImplementedException("Unknown metadata usage type: " + usage.Type);
        }

        // Get the type used in a metadata usage
        public TypeInfo GetMetadataUsageType(MetadataUsage usage) => usage.Type switch {
            MetadataUsageType.Type => TypesByReferenceIndex[usage.SourceIndex],
            MetadataUsageType.TypeInfo => TypesByReferenceIndex[usage.SourceIndex],
            MetadataUsageType.MethodDef => GetMetadataUsageMethod(usage).DeclaringType,
            MetadataUsageType.FieldInfo => TypesByReferenceIndex[Package.FieldRefs[usage.SourceIndex].typeIndex],
            MetadataUsageType.MethodRef => GetMetadataUsageMethod(usage).DeclaringType,

            _ => throw new InvalidOperationException("Incorrect metadata usage type to retrieve referenced type")
        };

        // Get the method used in a metadata usage
        public MethodBase GetMetadataUsageMethod(MetadataUsage usage) => usage.Type switch {
            MetadataUsageType.MethodDef => MethodsByDefinitionIndex[usage.SourceIndex],
            MetadataUsageType.MethodRef => Package.MethodSpecs[usage.SourceIndex].methodIndexIndex != -1?
                GenericMethods[Package.MethodSpecs[usage.SourceIndex]] :
                MethodsByDefinitionIndex[Package.MethodSpecs[usage.SourceIndex].methodDefinitionIndex],
            _ => throw new InvalidOperationException("Incorrect metadata usage type to retrieve referenced type")
        };
    }
}