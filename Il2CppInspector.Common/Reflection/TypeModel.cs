/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Il2CppInspector.Reflection
{
    public class TypeModel
    {
        public Il2CppInspector Package { get; }
        public List<Assembly> Assemblies { get; } = new List<Assembly>();

        // List of all namespaces defined by the application
        public List<string> Namespaces { get; }

        // List of all types from TypeDefs ordered by their TypeDefinitionIndex
        public TypeInfo[] TypesByDefinitionIndex { get; }

        // List of all types from TypeRefs ordered by instanceIndex
        public TypeInfo[] TypesByReferenceIndex { get; }

        // List of all types from GenericParameters
        public TypeInfo[] GenericParameterTypes { get; }

        // List of all methods from MethodSpecs (closed generic methods that can be called; does not need to be in a generic class)
        public Dictionary<Il2CppMethodSpec, MethodBase> GenericMethods { get; } = new Dictionary<Il2CppMethodSpec, MethodBase>();

        // List of all type definitions by fully qualified name (TypeDefs only)
        public Dictionary<string, TypeInfo> TypesByFullName { get; } = new Dictionary<string, TypeInfo>();

        // Every type
        public IEnumerable<TypeInfo> Types => TypesByDefinitionIndex.Concat(TypesByReferenceIndex)
            .Concat(GenericMethods.Values.Select(m => m.DeclaringType)).Distinct().Where(t => t != null);

        // List of all methods ordered by their MethodDefinitionIndex
        public MethodBase[] MethodsByDefinitionIndex { get; }

        // List of all Method.Invoke functions by invoker index
        public MethodInvoker[] MethodInvokers { get; }

        // List of all generated CustomAttributeData objects by their instanceIndex into AttributeTypeIndices
        public ConcurrentDictionary<int, CustomAttributeData> AttributesByIndices { get; } = new ConcurrentDictionary<int, CustomAttributeData>();

        // List of unique custom attributes generators indexed by type (multiple indices above may refer to a single generator function)
        public Dictionary<TypeInfo, List<CustomAttributeData>> CustomAttributeGenerators { get; }

        // List of unique custom attributes generators indexed by virtual address
        public Dictionary<ulong, List<CustomAttributeData>> CustomAttributeGeneratorsByAddress { get; }

        // Get an assembly by its image name
        public Assembly GetAssembly(string name) => Assemblies.FirstOrDefault(a => a.ShortName == name);

        // Get a type by its fully qualified name including generic type arguments, array brackets etc.
        // In other words, rather than only being able to fetch a type definition such as in Assembly.GetType(),
        // this method can also find reference types, types created from TypeRefs and constructed types from MethodSpecs
        public TypeInfo GetType(string fullName) => Types.FirstOrDefault(
            t => fullName == t.Namespace + (!string.IsNullOrEmpty(t.Namespace)? "." : "") + t.Name);

        // Get a concrete instantiation of a generic method from its fully qualified name and type arguments
        public MethodBase GetGenericMethod(string fullName, params TypeInfo[] typeArguments) =>
            GenericMethods.Values.First(
                m => fullName == m.DeclaringType.Namespace + (!string.IsNullOrEmpty(m.DeclaringType.Namespace)? "." : "")
                                    + m.DeclaringType.Name + "." + m.Name
                && m.GetGenericArguments().SequenceEqual(typeArguments));

        // Create type model
        public TypeModel(Il2CppInspector package) {
            Package = package;
            TypesByDefinitionIndex = new TypeInfo[package.TypeDefinitions.Length];
            TypesByReferenceIndex = new TypeInfo[package.TypeReferences.Count];
            GenericParameterTypes = new TypeInfo[package.GenericParameters.Length];
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
                var referencedType = resolveTypeReference(typeRef);

                TypesByReferenceIndex[typeRefIndex] = referencedType;
            }

            // Create types and methods from MethodSpec (which incorporates TypeSpec in IL2CPP)
            foreach (var spec in Package.MethodSpecs) {
                var methodDefinition = MethodsByDefinitionIndex[spec.methodDefinitionIndex];
                var declaringType = methodDefinition.DeclaringType;

                // Concrete instance of a generic class
                // If the class index is not specified, we will later create a generic method in a non-generic class
                if (spec.classIndexIndex != -1) {
                    var genericInstance = Package.GenericInstances[spec.classIndexIndex];
                    var genericArguments = ResolveGenericArguments(genericInstance);
                    declaringType = declaringType.MakeGenericType(genericArguments);
                }

                MethodBase method;
                if (methodDefinition is ConstructorInfo)
                    method = declaringType.GetConstructorByDefinition((ConstructorInfo)methodDefinition);
                else
                    method = declaringType.GetMethodByDefinition((MethodInfo)methodDefinition);

                if (spec.methodIndexIndex != -1) {
                    var genericInstance = Package.GenericInstances[spec.methodIndexIndex];
                    var genericArguments = ResolveGenericArguments(genericInstance);
                    method = method.MakeGenericMethod(genericArguments);
                }
                method.VirtualAddress = Package.GetGenericMethodPointer(spec);
                GenericMethods[spec] = method;
            }

            // Generate a list of all namespaces used
            Namespaces = Assemblies.SelectMany(x => x.DefinedTypes).GroupBy(t => t.Namespace).Select(n => n.Key).Distinct().ToList();

            // Find all custom attribute generators (populate AttributesByIndices) (use ToList() to force evaluation)
            var allAssemblyAttributes = Assemblies.Select(a => a.CustomAttributes).ToList();
            var allTypeAttributes = TypesByDefinitionIndex.Select(t => t.CustomAttributes).ToList();
            var allEventAttributes = TypesByDefinitionIndex.SelectMany(t => t.DeclaredEvents).Select(e => e.CustomAttributes).ToList();
            var allFieldAttributes = TypesByDefinitionIndex.SelectMany(t => t.DeclaredFields).Select(f => f.CustomAttributes).ToList();
            var allPropertyAttributes = TypesByDefinitionIndex.SelectMany(t => t.DeclaredProperties).Select(p => p.CustomAttributes).ToList();
            var allMethodAttributes = MethodsByDefinitionIndex.Select(m => m.CustomAttributes).ToList();
            var allParameterAttributes = MethodsByDefinitionIndex.SelectMany(m => m.DeclaredParameters).Select(p => p.CustomAttributes).ToList();

            // Populate list of unique custom attribute generators for each type
            CustomAttributeGenerators = AttributesByIndices.Values
                .GroupBy(a => a.AttributeType)
                .ToDictionary(g => g.Key, g => g.GroupBy(a => a.VirtualAddress.Start).Select(g => g.First()).ToList());

            // Populate list of unique custom attribute generators for each address
            CustomAttributeGeneratorsByAddress = AttributesByIndices.Values
                .GroupBy(a => a.VirtualAddress.Start)
                .ToDictionary(g => g.Key, g => g.GroupBy(a => a.AttributeType).Select(g => g.First()).ToList());

            // Create method invokers (one per signature, in invoker index order)
            // Generic type definitions have an invoker index of -1
            foreach (var method in MethodsByDefinitionIndex) {
                var index = package.GetInvokerIndex(method.DeclaringType.Assembly.ModuleDefinition, method.Definition);
                if (index != -1) {
                    if (MethodInvokers[index] == null)
                        MethodInvokers[index] = new MethodInvoker(method, index);

                    method.Invoker = MethodInvokers[index];
                }
            }

            // Create method invokers sourced from generic method invoker indices
            foreach (var spec in GenericMethods.Keys) {
                if (package.GenericMethodInvokerIndices.TryGetValue(spec, out var index)) {
                    if (MethodInvokers[index] == null)
                        MethodInvokers[index] = new MethodInvoker(GenericMethods[spec], index);

                    GenericMethods[spec].Invoker = MethodInvokers[index];
                }
            }

            // Post-processing hook
            PluginHooks.PostProcessTypeModel(this);
        }

        // Get generic arguments from either a type or method instanceIndex from a MethodSpec
        public TypeInfo[] ResolveGenericArguments(Il2CppGenericInst inst) {

            // Get list of pointers to type parameters (both unresolved and concrete)
            var genericTypeArguments = Package.BinaryImage.ReadMappedArray<ulong>(inst.type_argv, (int)inst.type_argc);
            
            return genericTypeArguments.Select(a => GetTypeFromVirtualAddress(a)).ToArray();
        }

        // Initialize type from type reference (TypeRef)
        // Much of the following is adapted from il2cpp::vm::Class::FromIl2CppType
        private TypeInfo resolveTypeReference(Il2CppType typeRef) {
            var image = Package.BinaryImage;
            TypeInfo underlyingType;

            switch (typeRef.type) {
                // Classes defined in the metadata (reference to a TypeDef)
                case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
                case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
                    underlyingType = TypesByDefinitionIndex[typeRef.datapoint]; // klassIndex
                    break;

                // Constructed types
                case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                    // TODO: Replace with array load from Il2CppMetadataRegistration.genericClasses
                    var generic = image.ReadMappedObject<Il2CppGenericClass>(typeRef.datapoint); // Il2CppGenericClass *

                    // Get generic type definition
                    TypeInfo genericTypeDef;
                    if (Package.Version < 27) {
                        // It appears that TypeRef can be -1 if the generic depth recursion limit
                        // (--maximum-recursive-generic-depth=) is reached in Il2Cpp. In this case,
                        // no generic instance type is generated, so we just produce a null TypeInfo here.
                        if ((generic.typeDefinitionIndex & 0xffff_ffff) == 0x0000_0000_ffff_ffff)
                            return null;

                        genericTypeDef = TypesByDefinitionIndex[generic.typeDefinitionIndex];
                    } else {
                        genericTypeDef = GetTypeFromVirtualAddress(generic.type);
                    }

                    // Get the instantiation
                    // TODO: Replace with array load from Il2CppMetadataRegistration.genericInsts
                    var genericInstance = image.ReadMappedObject<Il2CppGenericInst>(generic.context.class_inst);
                    var genericArguments = ResolveGenericArguments(genericInstance);

                    underlyingType = genericTypeDef.MakeGenericType(genericArguments);
                    break;
                case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
                    var descriptor = image.ReadMappedObject<Il2CppArrayType>(typeRef.datapoint);
                    var elementType = GetTypeFromVirtualAddress(descriptor.etype);
                    underlyingType = elementType.MakeArrayType(descriptor.rank);
                    break;
                case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                    elementType = GetTypeFromVirtualAddress(typeRef.datapoint);
                    underlyingType = elementType.MakeArrayType(1);
                    break;
                case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
                    elementType = GetTypeFromVirtualAddress(typeRef.datapoint);
                    underlyingType = elementType.MakePointerType();
                    break;

                // Generic type and generic method parameters
                case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
                case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
                    underlyingType = GetGenericParameterType((int)typeRef.datapoint);
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
            if ((int)t >= Il2CppConstants.FullNameTypeString.Count)
                return null;

            var fqn = Il2CppConstants.FullNameTypeString[(int)t];
            return TypesByFullName[fqn];
        }

        // Get a TypeRef by its virtual address
        // These are always nested types from references within another TypeRef
        public TypeInfo GetTypeFromVirtualAddress(ulong ptr) {
            var typeRefIndex = Package.TypeReferenceIndicesByAddress[ptr];

            if (TypesByReferenceIndex[typeRefIndex] != null)
                return TypesByReferenceIndex[typeRefIndex];

            var type = Package.TypeReferences[typeRefIndex];
            var referencedType = resolveTypeReference(type);

            TypesByReferenceIndex[typeRefIndex] = referencedType;
            return referencedType;
        }

        public TypeInfo GetGenericParameterType(int index) {
            if (GenericParameterTypes[index] != null)
                return GenericParameterTypes[index];

            var paramType = Package.GenericParameters[index]; // genericParameterIndex
            var container = Package.GenericContainers[paramType.ownerIndex];
            TypeInfo result;

            if (container.is_method == 1) {
                var owner = MethodsByDefinitionIndex[container.ownerIndex];
                result = new TypeInfo(owner, paramType);
            } else {
                var owner = TypesByDefinitionIndex[container.ownerIndex];
                result = new TypeInfo(owner, paramType);
            }
            GenericParameterTypes[index] = result;
            return result;
        }

        // The attribute index is an index into AttributeTypeRanges, each of which is a start-end range index into AttributeTypeIndices, each of which is a TypeIndex
        public int GetCustomAttributeIndex(Assembly asm, int token, int customAttributeIndex) {
            // Prior to v24.1, Type, Field, Parameter, Method, Event, Property, Assembly definitions had their own customAttributeIndex field
            if (Package.Version <= 24.0)
                return customAttributeIndex;

            if (Package.Version >= 29)
                //V29 changed this
                return -1;

            // From v24.1 onwards, token was added to Il2CppCustomAttributeTypeRange and each Il2CppImageDefinition noted the CustomAttributeTypeRanges for the image
            if (!Package.AttributeIndicesByToken[asm.ImageDefinition.customAttributeStart].TryGetValue((uint) token, out var index))
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
            MetadataUsageType.MethodRef => GenericMethods[Package.MethodSpecs[usage.SourceIndex]],
            _ => throw new InvalidOperationException("Incorrect metadata usage type to retrieve referenced type")
        };
    }
}